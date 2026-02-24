using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using TemporalDashboard.WorkflowDiagramming;
using TemporalDashboard.WorkflowDiagramming.Attributes;
using Temporalio.Workflows;
using WorkflowInfo = TemporalDashboard.Api.Models.WorkflowInfo;
using WorkflowTypeInfo = TemporalDashboard.Api.Models.WorkflowTypeInfo;
using WorkflowDllInfo = TemporalDashboard.Api.Models.WorkflowDllInfo;
using WorkflowDiagramsMetadataDto = TemporalDashboard.Api.Models.WorkflowDiagramsMetadataDto;

namespace TemporalDashboard.Api.Services;

/// <summary>
/// Isolated load context so multiple DLLs with the same assembly name (from different paths) can be loaded without collision.
/// We intentionally do not load our diagramming lib or Temporalio here so they stay in the default context; otherwise
/// attribute types would differ and GetCustomAttribute&lt;WorkflowStepAttribute&gt;() would not find the workflow's attributes.
/// </summary>
internal sealed class IsolatedDllLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public IsolatedDllLoadContext(string mainAssemblyPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? "";
        // Resolve diagramming and Temporal from default context so attribute reflection matches
        if (name.StartsWith("TemporalDashboard.WorkflowDiagramming", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Temporalio.", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}

public class WorkflowDiscoveryService
{
    private const string DiagramsFolderName = "diagrams";
    private const string MetadataFileName = "workflow-diagrams-metadata.json";
    private const string DiagramsPackagePrefix = "diagrams-";

    private readonly string _uploadsPath;
    private readonly string _diagramsPath;
    private readonly ILogger<WorkflowDiscoveryService> _logger;
    private readonly object _cacheLock = new();
    private List<WorkflowInfo>? _workflowsCache;

    public WorkflowDiscoveryService(IConfiguration configuration, ILogger<WorkflowDiscoveryService> logger)
    {
        _uploadsPath = configuration["UploadsPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _diagramsPath = Path.Combine(_uploadsPath, DiagramsFolderName);
        _logger = logger;
        
        // Ensure uploads directory exists
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    public string GetUploadsPath() => _uploadsPath;
    public string GetDiagramsPath() => _diagramsPath;

    /// <summary>
    /// Invalidates any in-memory cache so that the next discovery or diagram request will load assemblies from disk.
    /// Call this after uploading or replacing workflow DLLs.
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _workflowsCache = null;
        }
        _logger.LogDebug("Workflow discovery cache invalidated.");
    }

    /// <summary>
    /// Discovers all workflow types from DLLs in the uploads folder and from diagram packages (JSON + Mermaid) in uploads/diagrams.
    /// Results are cached until <see cref="InvalidateCache"/> is called (e.g. after an upload).
    /// Diagram packages are exposed with DllName "diagrams-&lt;assemblyName&gt;" so they do not collide with DLL-based uploads.
    /// </summary>
    public List<WorkflowInfo> DiscoverWorkflows()
    {
        lock (_cacheLock)
        {
            if (_workflowsCache != null)
                return new List<WorkflowInfo>(_workflowsCache);
        }

        var workflows = new List<WorkflowInfo>();
        if (!Directory.Exists(_uploadsPath))
            return workflows;

        // From DLL folders (uploads/<AssemblyName>/<AssemblyName>.dll)
        foreach (var folderPath in Directory.GetDirectories(_uploadsPath))
        {
            var folderName = Path.GetFileName(folderPath);
            if (folderName.Equals(DiagramsFolderName, StringComparison.OrdinalIgnoreCase))
                continue;
            var mainDllPath = Path.Combine(folderPath, folderName + ".dll");
            if (!File.Exists(mainDllPath) || ShouldSkipAssemblyPath(mainDllPath))
                continue;
            try
            {
                DiscoverWorkflowsFromAssemblyPath(mainDllPath, workflows);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load assembly from {DllPath}", mainDllPath);
            }
        }

        // From diagram packages (uploads/diagrams/<assemblyName>/)
        DiscoverWorkflowsFromDiagramsPackages(workflows);

        lock (_cacheLock)
        {
            _workflowsCache = new List<WorkflowInfo>(workflows);
        }
        return workflows;
    }

    private void DiscoverWorkflowsFromDiagramsPackages(List<WorkflowInfo> workflows)
    {
        if (!Directory.Exists(_diagramsPath))
            return;
        foreach (var packagePath in Directory.GetDirectories(_diagramsPath))
        {
            var metadataPath = Path.Combine(packagePath, MetadataFileName);
            if (!File.Exists(metadataPath))
                continue;
            try
            {
                var json = File.ReadAllText(metadataPath);
                var meta = JsonSerializer.Deserialize<WorkflowDiagramsMetadataDto>(json);
                if (meta?.Workflows == null || meta.Workflows.Count == 0)
                    continue;
                var assemblyName = Path.GetFileName(packagePath);
                var dllName = DiagramsPackagePrefix + assemblyName;
                foreach (var w in meta.Workflows)
                {
                    workflows.Add(new WorkflowInfo
                    {
                        DllName = dllName,
                        DllPath = packagePath,
                        WorkflowName = w.Name,
                        DisplayName = w.DisplayName ?? string.Empty,
                        FullName = w.Name,
                        Namespace = string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read diagram package metadata from {Path}", metadataPath);
            }
        }
    }

    /// <summary>
    /// Discovers workflows from DLLs in a specific directory path
    /// </summary>
    public List<WorkflowInfo> DiscoverWorkflowsFromPath(string directoryPath)
    {
        var workflows = new List<WorkflowInfo>();
        
        if (!Directory.Exists(directoryPath))
        {
            return workflows;
        }

        var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories)
            .Where(p => !ShouldSkipAssemblyPath(p))
            .ToList();

        foreach (var dllPath in dllFiles)
        {
            try
            {
                DiscoverWorkflowsFromAssemblyPath(dllPath, workflows);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load assembly from {DllPath}", dllPath);
            }
        }

        return workflows;
    }

    /// <summary>
    /// Loads the DLL in an isolated ALC, discovers workflow types, adds to the list, then unloads. Avoids "assembly already loaded" when the same assembly name exists in multiple folders.
    /// </summary>
    private void DiscoverWorkflowsFromAssemblyPath(string dllPath, List<WorkflowInfo> workflows)
    {
        var alc = new IsolatedDllLoadContext(dllPath);
        try
        {
            var assembly = alc.LoadFromAssemblyPath(dllPath);
            var workflowTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<WorkflowAttribute>() != null)
                .ToList();

            foreach (var workflowType in workflowTypes)
            {
                var diagramAttr = workflowType.GetCustomAttribute<WorkflowDiagramAttribute>();
                workflows.Add(new WorkflowInfo
                {
                    DllName = Path.GetFileName(dllPath),
                    DllPath = dllPath,
                    WorkflowName = workflowType.Name,
                    DisplayName = diagramAttr?.DisplayName ?? string.Empty,
                    FullName = workflowType.FullName ?? workflowType.Name,
                    Namespace = workflowType.Namespace ?? string.Empty
                });
            }
        }
        finally
        {
            alc.Unload();
        }
    }

    /// <summary>
    /// Returns DLLs in the given path that contain at least one workflow type (for upload filtering).
    /// Each entry includes the DLL path, assembly name, and source directory so only workflow-containing assemblies can be saved.
    /// </summary>
    public List<WorkflowDllInfo> GetWorkflowDllInfos(string extractPath)
    {
        var result = new List<WorkflowDllInfo>();
        if (!Directory.Exists(extractPath))
            return result;

        var dllFiles = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories)
            .Where(p => !ShouldSkipAssemblyPath(p))
            .ToList();

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var alc = new IsolatedDllLoadContext(dllPath);
                try
                {
                    var assembly = alc.LoadFromAssemblyPath(dllPath);
                    var hasWorkflows = assembly.GetTypes()
                        .Any(t => t.GetCustomAttribute<WorkflowAttribute>() != null);
                    if (hasWorkflows)
                    {
                        var assemblyName = Path.GetFileNameWithoutExtension(dllPath);
                        var sourceDir = Path.GetDirectoryName(dllPath) ?? extractPath;
                        result.Add(new WorkflowDllInfo
                        {
                            DllPath = dllPath,
                            AssemblyName = assemblyName,
                            SourceDirectory = sourceDir
                        });
                    }
                }
                finally
                {
                    alc.Unload();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping assembly (could not load): {DllPath}", dllPath);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns true for paths that should not be loaded as assemblies (e.g. __MACOSX, ._* AppleDouble files).
    /// </summary>
    private static bool ShouldSkipAssemblyPath(string dllPath)
    {
        var path = dllPath.Replace('\\', '/');
        if (path.Contains("__MACOSX/", StringComparison.OrdinalIgnoreCase))
            return true;
        var fileName = Path.GetFileName(dllPath);
        if (fileName.StartsWith("._", StringComparison.Ordinal))
            return true;
        return false;
    }

    /// <summary>
    /// Gets all workflows from a specific DLL or diagram package. dllName may be a DLL identifier (e.g. MyWorkflows) or a diagram package (diagrams-MyWorkflows).
    /// </summary>
    public List<WorkflowTypeInfo> GetWorkflowsFromDll(string dllName)
    {
        if (dllName.StartsWith(DiagramsPackagePrefix, StringComparison.Ordinal))
        {
            var assemblyName = dllName.Substring(DiagramsPackagePrefix.Length);
            return GetWorkflowsFromDiagramsPackage(assemblyName);
        }

        var assemblyFolderName = Path.GetFileNameWithoutExtension(dllName);
        var preferredPath = Path.Combine(_uploadsPath, assemblyFolderName, dllName);
        var dllPath = File.Exists(preferredPath) && !ShouldSkipAssemblyPath(preferredPath)
            ? preferredPath
            : null;

        if (dllPath == null)
        {
            dllPath = Directory.GetFiles(_uploadsPath, dllName, SearchOption.AllDirectories)
                .Where(p => !ShouldSkipAssemblyPath(p))
                .FirstOrDefault();
        }

        if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
        {
            throw new FileNotFoundException($"DLL not found: {dllName}");
        }

        var workflows = new List<WorkflowTypeInfo>();
        var alc = new IsolatedDllLoadContext(dllPath);
        try
        {
            var assembly = alc.LoadFromAssemblyPath(dllPath);
            var workflowTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<WorkflowAttribute>() != null)
                .ToList();

            foreach (var workflowType in workflowTypes)
            {
                var mermaidDiagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
                workflows.Add(new WorkflowTypeInfo
                {
                    Name = workflowType.Name,
                    FullName = workflowType.FullName ?? workflowType.Name,
                    Namespace = workflowType.Namespace ?? string.Empty,
                    MermaidDiagram = mermaidDiagram
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflows from {DllPath}", dllPath);
            throw;
        }
        finally
        {
            alc.Unload();
        }

        return workflows;
    }

    private List<WorkflowTypeInfo> GetWorkflowsFromDiagramsPackage(string assemblyName)
    {
        var packagePath = Path.Combine(_diagramsPath, SanitizeFolderName(assemblyName));
        var metadataPath = Path.Combine(packagePath, MetadataFileName);
        if (!File.Exists(metadataPath))
            throw new FileNotFoundException($"Diagram package not found: {assemblyName}");

        var json = File.ReadAllText(metadataPath);
        var meta = JsonSerializer.Deserialize<WorkflowDiagramsMetadataDto>(json);
        if (meta?.Workflows == null)
            throw new FileNotFoundException($"Invalid diagram package: {assemblyName}");

        var result = new List<WorkflowTypeInfo>();
        foreach (var w in meta.Workflows)
        {
            var mermaidPath = Path.Combine(packagePath, w.DiagramFile);
            var mermaid = File.Exists(mermaidPath) ? File.ReadAllText(mermaidPath) : string.Empty;
            result.Add(new WorkflowTypeInfo
            {
                Name = w.Name,
                FullName = w.Name,
                Namespace = string.Empty,
                MermaidDiagram = mermaid
            });
        }
        return result;
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Gets a single workflow's diagram by DLL/diagram-package name and workflow type name.
    /// </summary>
    public WorkflowTypeInfo GetWorkflowDiagram(string dllName, string workflowName)
    {
        var all = GetWorkflowsFromDll(dllName);
        var match = all.FirstOrDefault(w => string.Equals(w.Name, workflowName, StringComparison.Ordinal));
        if (match == null)
            throw new FileNotFoundException($"Workflow '{workflowName}' not found in {dllName}");
        return match;
    }
}
