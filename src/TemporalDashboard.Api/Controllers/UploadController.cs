using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TemporalDashboard.Api.Models;
using TemporalDashboard.Api.Services;

namespace TemporalDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly WorkflowDiscoveryService _discoveryService;
    private readonly ILogger<UploadController> _logger;
    private readonly string _uploadsPath;

    public UploadController(WorkflowDiscoveryService discoveryService, ILogger<UploadController> logger)
    {
        _discoveryService = discoveryService;
        _logger = logger;
        _uploadsPath = _discoveryService.GetUploadsPath();
    }

    /// <summary>
    /// Uploads a zip file, extracts to temp, keeps only DLLs that contain workflows, and saves each in a folder named by assembly (file-name safe).
    /// If overwrite=false and any target assembly folder already exists, returns 409 so the client can prompt the user.
    /// </summary>
    [HttpPost("zip")]
    public async Task<IActionResult> UploadZip(IFormFile file, [FromQuery] bool overwrite = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be a zip archive" });

        var tempZipPath = Path.Combine(Path.GetTempPath(), "TemporalDashboard", $"{Guid.NewGuid()}.zip");
        var tempExtractPath = Path.Combine(Path.GetTempPath(), "TemporalDashboard", Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempZipPath)!);
            Directory.CreateDirectory(tempExtractPath);

            using (var stream = new FileStream(tempZipPath, FileMode.Create))
                await file.CopyToAsync(stream);

            ExtractZipExcludingJunk(tempZipPath, tempExtractPath);

            // Check for workflow-diagrams-metadata.json (diagrams package from build task)
            var metadataPath = FindFileRecursive(tempExtractPath, "workflow-diagrams-metadata.json");
            if (metadataPath != null)
            {
                var diagramsResult = await SaveDiagramsPackageAsync(metadataPath, tempExtractPath, file.FileName, overwrite);
                if (diagramsResult.Conflict)
                {
                    CleanupTemp(tempZipPath, tempExtractPath);
                    return StatusCode(409, new
                    {
                        error = "Diagram package already exists",
                        message = "A diagram package with this assembly name already exists. Choose to overwrite or cancel.",
                        existingAssemblies = diagramsResult.ExistingNames
                    });
                }
                if (diagramsResult.Saved)
                {
                    CleanupTemp(tempZipPath, tempExtractPath);
                    _discoveryService.InvalidateCache();
                    _logger.LogInformation("Saved diagram package {AssemblyName} from {FileName}, {WorkflowCount} workflow(s).",
                        diagramsResult.AssemblyName, file.FileName, diagramsResult.WorkflowCount);
                    return Ok(new
                    {
                        message = "Upload successful. Diagram package (JSON + Mermaid) was saved.",
                        savedAssemblies = new[] { diagramsResult.AssemblyName },
                        workflowCount = diagramsResult.WorkflowCount,
                        fileName = file.FileName,
                        uploadType = "diagrams"
                    });
                }
            }

            var workflowDlls = _discoveryService.GetWorkflowDllInfos(tempExtractPath);
            if (workflowDlls.Count == 0)
            {
                CleanupTemp(tempZipPath, tempExtractPath);
                _logger.LogWarning("No workflow-containing DLLs or diagram package found in {FileName}.", file.FileName);
                return BadRequest(new
                {
                    error = "No workflows found",
                    message = "The zip must contain either (1) workflow-diagrams-metadata.json plus .mermaid files, or (2) DLLs with Temporal workflows ([Workflow] types)."
                });
            }

            var workflowCount = _discoveryService.DiscoverWorkflowsFromPath(tempExtractPath).Count;

            var folderNames = workflowDlls
                .Select(d => SanitizeFolderName(d.AssemblyName))
                .Distinct()
                .ToList();

            var existing = folderNames
                .Where(f => Directory.Exists(Path.Combine(_uploadsPath, f)))
                .ToList();

            if (existing.Count > 0 && !overwrite)
            {
                CleanupTemp(tempZipPath, tempExtractPath);
                return StatusCode(409, new
                {
                    error = "Assemblies already exist",
                    message = "One or more workflow assemblies from this zip already exist. Choose to overwrite or cancel.",
                    existingAssemblies = existing
                });
            }

            if (!Directory.Exists(_uploadsPath))
                Directory.CreateDirectory(_uploadsPath);

            foreach (var dllInfo in workflowDlls)
            {
                var targetFolder = SanitizeFolderName(dllInfo.AssemblyName);
                var targetPath = Path.Combine(_uploadsPath, targetFolder);

                if (existing.Contains(targetFolder))
                {
                    try
                    {
                        Directory.Delete(targetPath, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete existing folder {TargetPath}", targetPath);
                    }
                }

                Directory.CreateDirectory(targetPath);
                var dllsInDir = Directory.GetFiles(dllInfo.SourceDirectory, "*.dll");
                foreach (var dll in dllsInDir)
                {
                    if (ShouldSkipAssemblyPath(dll))
                        continue;
                    var destPath = Path.Combine(targetPath, Path.GetFileName(dll));
                    System.IO.File.Copy(dll, destPath, overwrite: true);
                }
            }

            CleanupTemp(tempZipPath, tempExtractPath);

            _discoveryService.InvalidateCache();

            _logger.LogInformation("Saved {AssemblyCount} workflow assembly folder(s) from {FileName}, {WorkflowCount} workflow(s).",
                folderNames.Count, file.FileName, workflowCount);

            return Ok(new
            {
                message = "Upload successful. Only workflow-containing assemblies were saved.",
                savedAssemblies = folderNames,
                workflowCount,
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            CleanupTemp(tempZipPath, tempExtractPath);
            _logger.LogError(ex, "Error uploading zip {FileName}", file.FileName);
            return StatusCode(500, new { error = "Failed to upload", message = ex.Message });
        }
    }

    private static void CleanupTemp(string? zipPath, string? extractPath)
    {
        try
        {
            if (extractPath != null && Directory.Exists(extractPath))
                Directory.Delete(extractPath, recursive: true);
        }
        catch { /* best effort */ }
        try
        {
            if (zipPath != null && System.IO.File.Exists(zipPath))
                System.IO.File.Delete(zipPath);
        }
        catch { /* best effort */ }
    }

    private static string SanitizeFolderName(string assemblyName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", assemblyName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool ShouldSkipAssemblyPath(string dllPath)
    {
        var path = dllPath.Replace('\\', '/');
        if (path.Contains("__MACOSX/", StringComparison.OrdinalIgnoreCase))
            return true;
        var fileName = Path.GetFileName(dllPath);
        return fileName.StartsWith("._", StringComparison.Ordinal);
    }

    /// <summary>
    /// Extracts a zip to the target directory, skipping __MACOSX and ._* (AppleDouble) entries.
    /// </summary>
    private static void ExtractZipExcludingJunk(string zipPath, string extractPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var fullName = entry.FullName.Replace('\\', '/');
            if (ShouldSkipZipEntry(fullName))
                continue;
            var destPath = Path.Combine(extractPath, entry.FullName);
            if (string.IsNullOrEmpty(entry.Name))
            {
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);
                continue;
            }
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            entry.ExtractToFile(destPath, overwrite: true);
        }
    }

    private static bool ShouldSkipZipEntry(string fullName)
    {
        var normalized = fullName.Replace('\\', '/').TrimStart('/');
        var segments = normalized.Split('/', '\\');
        foreach (var segment in segments)
        {
            if (segment.Equals("__MACOSX", StringComparison.OrdinalIgnoreCase))
                return true;
            if (segment.StartsWith("._", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static string? FindFileRecursive(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (System.IO.File.Exists(path))
            return path;
        foreach (var dir in Directory.GetDirectories(directory))
        {
            var found = FindFileRecursive(dir, fileName);
            if (found != null)
                return found;
        }
        return null;
    }

    private async Task<(bool Saved, bool Conflict, string AssemblyName, int WorkflowCount, List<string> ExistingNames)> SaveDiagramsPackageAsync(
        string metadataPath, string extractPath, string fileName, bool overwrite)
    {
        string json;
        try
        {
            json = await System.IO.File.ReadAllTextAsync(metadataPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read metadata at {Path}", metadataPath);
            return (false, false, string.Empty, 0, new List<string>());
        }

        WorkflowDiagramsMetadataDto? meta;
        try
        {
            meta = JsonSerializer.Deserialize<WorkflowDiagramsMetadataDto>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse workflow-diagrams-metadata.json");
            return (false, false, string.Empty, 0, new List<string>());
        }

        if (meta?.Workflows == null || meta.Workflows.Count == 0)
            return (false, false, string.Empty, 0, new List<string>());

        var assemblyName = (meta.AssemblyName ?? Path.GetFileName(Path.GetDirectoryName(metadataPath)) ?? "Unknown").Trim();
        if (string.IsNullOrEmpty(assemblyName))
            return (false, false, string.Empty, 0, new List<string>());

        var targetFolder = SanitizeFolderName(assemblyName);
        var diagramsPath = _discoveryService.GetDiagramsPath();
        var targetPath = Path.Combine(diagramsPath, targetFolder);
        var existing = Directory.Exists(targetPath);
        if (existing && !overwrite)
            return (false, true, assemblyName, meta.Workflows.Count, new List<string> { assemblyName });

        if (existing)
        {
            try
            {
                Directory.Delete(targetPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete existing diagram package {Path}", targetPath);
                return (false, false, assemblyName, 0, new List<string>());
            }
        }

        Directory.CreateDirectory(targetPath);
        var metadataDest = Path.Combine(targetPath, "workflow-diagrams-metadata.json");
        System.IO.File.Copy(metadataPath, metadataDest, overwrite: true);

        var metadataDir = Path.GetDirectoryName(metadataPath) ?? extractPath;
        foreach (var w in meta.Workflows)
        {
            if (string.IsNullOrEmpty(w.DiagramFile))
                continue;
            var srcPath = Path.Combine(metadataDir, w.DiagramFile);
            if (!System.IO.File.Exists(srcPath))
                srcPath = Path.Combine(extractPath, w.DiagramFile);
            if (System.IO.File.Exists(srcPath))
            {
                var destPath = Path.Combine(targetPath, Path.GetFileName(w.DiagramFile));
                System.IO.File.Copy(srcPath, destPath, overwrite: true);
            }
        }

        return (true, false, assemblyName, meta.Workflows.Count, new List<string>());
    }

    /// <summary>
    /// Gets information about uploaded files
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetUploadInfo()
    {
        try
        {
            if (!Directory.Exists(_uploadsPath))
            {
                return Ok(new 
                { 
                    uploadsPath = _uploadsPath,
                    folderCount = 0,
                    totalDllCount = 0
                });
            }

            var folders = Directory.GetDirectories(_uploadsPath);
            var totalDllCount = Directory.GetFiles(_uploadsPath, "*.dll", SearchOption.AllDirectories).Length;

            return Ok(new 
            { 
                uploadsPath = _uploadsPath,
                folderCount = folders.Length,
                totalDllCount = totalDllCount,
                folders = folders.Select(f => new 
                { 
                    name = Path.GetFileName(f),
                    path = f,
                    dllCount = Directory.GetFiles(f, "*.dll", SearchOption.AllDirectories).Length
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload info");
            return StatusCode(500, new { error = "Failed to get upload info", message = ex.Message });
        }
    }
}
