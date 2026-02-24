using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using TemporalDashboard.WorkflowDiagramming;
using TemporalDashboard.WorkflowDiagramming.Attributes;
using Temporalio.Workflows;

namespace TemporalDashboard.WorkflowDiagramming.Build;

/// <summary>
/// MSBuild task that loads a built workflow assembly and generates Mermaid diagram files,
/// a JSON metadata file, and a zip archive for easy sharing.
/// </summary>
public sealed class GenerateWorkflowDiagramsTask : Microsoft.Build.Utilities.Task
{
    private const string MetadataFileName = "workflow-diagrams-metadata.json";
    private const string ZipFileName = "workflow-diagrams.zip";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Full path to the built assembly (e.g. the workflow project's output DLL).
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory where .mermaid files, metadata JSON, and zip will be written. Created if it does not exist.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// File extension for generated diagram files (e.g. ".mermaid" or ".md"). Defaults to ".mermaid".
    /// </summary>
    public string FileExtension { get; set; } = ".mermaid";

    /// <summary>
    /// Target framework (e.g. net10.0). Optional; included in metadata when set.
    /// </summary>
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>
    /// Language (e.g. C#). Optional; defaults to "C#" in metadata.
    /// </summary>
    public string Language { get; set; } = "C#";

    /// <summary>
    /// When true (default), creates workflow-diagrams.zip containing all diagrams and the metadata JSON.
    /// </summary>
    public bool CreateZip { get; set; } = true;

    public override bool Execute()
    {
        if (string.IsNullOrWhiteSpace(AssemblyPath))
        {
            Log.LogError("AssemblyPath must be set.");
            return false;
        }

        var fullAssemblyPath = Path.GetFullPath(AssemblyPath);
        if (!File.Exists(fullAssemblyPath))
        {
            Log.LogError("Assembly not found: {0}", fullAssemblyPath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            Log.LogError("OutputPath must be set.");
            return false;
        }

        var ext = FileExtension?.TrimStart('.');
        if (string.IsNullOrEmpty(ext))
            ext = "mermaid";
        var extension = ext.StartsWith('.') ? ext : "." + ext;

        var outputDir = Path.GetFullPath(OutputPath);
        try
        {
            Directory.CreateDirectory(outputDir);
        }
        catch (Exception ex)
        {
            Log.LogError("Failed to create output directory '{0}': {1}", outputDir, ex.Message);
            return false;
        }

        var taskDir = Path.GetDirectoryName(typeof(GenerateWorkflowDiagramsTask).Assembly.Location) ?? string.Empty;
        Func<AssemblyLoadContext, AssemblyName, Assembly?>? resolveHandler = null;
        try
        {
            // Ensure the default context can resolve diagramming/Temporal from the task's directory (NuGet package lib folder).
            // Without this, loading the user's assembly in the ALC can trigger a resolve in the default context that fails.
            resolveHandler = (_, name) =>
            {
                var simpleName = name.Name;
                if (string.IsNullOrEmpty(simpleName)) return null;
                if (!simpleName.StartsWith("TemporalDashboard.WorkflowDiagramming", StringComparison.OrdinalIgnoreCase) &&
                    !simpleName.StartsWith("Temporalio.", StringComparison.OrdinalIgnoreCase))
                    return null;
                var dllPath = Path.Combine(taskDir, simpleName + ".dll");
                return System.IO.File.Exists(dllPath) ? Assembly.LoadFrom(dllPath) : null;
            };
            AssemblyLoadContext.Default.Resolving += resolveHandler;

            var alc = new IsolatedDllLoadContext(fullAssemblyPath);
            try
            {
                var assembly = alc.LoadFromAssemblyPath(fullAssemblyPath);
                var workflowTypes = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<WorkflowAttribute>() != null)
                    .ToList();

                if (workflowTypes.Count == 0)
                {
                    Log.LogMessage(MessageImportance.Low, "No workflow types found in {0}. Skipping diagram generation.", fullAssemblyPath);
                    return true;
                }

                var workflowEntries = new List<WorkflowEntry>();
                var generated = 0;

                foreach (var workflowType in workflowTypes)
                {
                    try
                    {
                        var mermaid = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
                        var safeName = SanitizeFileName(workflowType.Name);
                        var diagramFileName = safeName + extension;
                        var filePath = Path.Combine(outputDir, diagramFileName);
                        File.WriteAllText(filePath, mermaid, System.Text.Encoding.UTF8);

                        var diagramAttr = workflowType.GetCustomAttribute<WorkflowDiagramAttribute>();
                        workflowEntries.Add(new WorkflowEntry
                        {
                            Name = workflowType.Name,
                            DisplayName = string.IsNullOrWhiteSpace(diagramAttr?.DisplayName) ? null : diagramAttr.DisplayName,
                            DiagramFile = diagramFileName
                        });
                        generated++;
                        Log.LogMessage(MessageImportance.Normal, "Generated diagram: {0}", filePath);
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning("Failed to generate diagram for workflow '{0}': {1}", workflowType.Name, ex.Message);
                    }
                }

                var assemblyName = assembly.GetName();
                var metadata = new WorkflowDiagramsMetadata
                {
                    AssemblyName = assemblyName.Name ?? Path.GetFileNameWithoutExtension(fullAssemblyPath),
                    AssemblyVersion = assemblyName.Version?.ToString() ?? "0.0.0.0",
                    AssemblyPath = fullAssemblyPath,
                    Language = string.IsNullOrWhiteSpace(Language) ? "C#" : Language.Trim(),
                    TargetFramework = TargetFramework?.Trim() ?? string.Empty,
                    BuildDateUtc = DateTime.UtcNow.ToString("O"),
                    Generator = "TemporalDashboard.WorkflowDiagramming.Build/1.0.0",
                    Workflows = workflowEntries
                };

                var metadataPath = Path.Combine(outputDir, MetadataFileName);
                File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, JsonOptions), System.Text.Encoding.UTF8);
                Log.LogMessage(MessageImportance.Normal, "Generated metadata: {0}", metadataPath);

                if (CreateZip)
                {
                    var zipPath = Path.Combine(outputDir, ZipFileName);
                    CreateZipArchive(outputDir, zipPath, extension, metadataPath);
                    Log.LogMessage(MessageImportance.Normal, "Generated zip: {0}", zipPath);
                }

                Log.LogMessage(MessageImportance.High, "Generated {0} workflow diagram(s), metadata, and zip in {1}", generated, outputDir);
            }
            finally
            {
                alc.Unload();
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogError("Failed to generate workflow diagrams from {0}: {1}", fullAssemblyPath, ex.Message);
            return false;
        }
        finally
        {
            if (resolveHandler != null)
                AssemblyLoadContext.Default.Resolving -= resolveHandler;
        }
    }

    private void CreateZipArchive(string outputDir, string zipPath, string diagramExtension, string metadataPath)
    {
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var dirInfo = new DirectoryInfo(outputDir);

        foreach (var file in dirInfo.EnumerateFiles("*" + diagramExtension))
            zip.CreateEntryFromFile(file.FullName, file.Name, CompressionLevel.Optimal);

        if (File.Exists(metadataPath))
            zip.CreateEntryFromFile(metadataPath, Path.GetFileName(metadataPath), CompressionLevel.Optimal);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
