using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
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

            var workflowDlls = _discoveryService.GetWorkflowDllInfos(tempExtractPath);
            if (workflowDlls.Count == 0)
            {
                CleanupTemp(tempZipPath, tempExtractPath);
                _logger.LogWarning("No workflow-containing DLLs found in {FileName}.", file.FileName);
                return BadRequest(new
                {
                    error = "No workflows found",
                    message = "The zip does not contain any DLLs with Temporal workflows. Ensure DLLs have classes marked with [Workflow]."
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
