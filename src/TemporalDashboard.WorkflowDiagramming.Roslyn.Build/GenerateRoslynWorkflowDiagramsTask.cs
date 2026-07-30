using System.IO.Compression;
using System.Text.Json;
using Microsoft.Build.Framework;
using TemporalDashboard.WorkflowDiagramming.Roslyn;

namespace TemporalDashboard.WorkflowDiagramming.Roslyn.Build;

/// <summary>
/// MSBuild task that analyzes Temporal workflow C# source with Roslyn and writes Mermaid
/// diagram files, JSON metadata, and a zip archive — no diagramming attributes required.
/// </summary>
public sealed class GenerateRoslynWorkflowDiagramsTask : Microsoft.Build.Utilities.Task
{
    private const string MetadataFileName = "workflow-diagrams-metadata.json";
    private const string ZipFileName = "workflow-diagrams.zip";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>C# source files to analyze (typically <c>@(Compile)</c>).</summary>
    [Required]
    public ITaskItem[] SourceFiles { get; set; } = [];

    /// <summary>Project directory used to resolve relative source paths.</summary>
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>Directory where .mermaid files, metadata JSON, and zip will be written.</summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>File extension for generated diagram files. Defaults to ".mermaid".</summary>
    public string FileExtension { get; set; } = ".mermaid";

    /// <summary>Target framework (e.g. net10.0). Optional; included in metadata when set.</summary>
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>Language (e.g. C#). Optional; defaults to "C#" in metadata.</summary>
    public string Language { get; set; } = "C#";

    /// <summary>Assembly / project name for metadata.</summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>Assembly version for metadata.</summary>
    public string AssemblyVersion { get; set; } = string.Empty;

    /// <summary>When true (default), creates workflow-diagrams.zip.</summary>
    public bool CreateZip { get; set; } = true;

    public override bool Execute()
    {
        if (SourceFiles == null || SourceFiles.Length == 0)
        {
            Log.LogWarning("GenerateRoslynWorkflowDiagrams: no SourceFiles provided; skipping.");
            return true;
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

        var projectDir = string.IsNullOrWhiteSpace(ProjectDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(ProjectDirectory);

        var sources = new List<(string path, string text)>();
        foreach (var item in SourceFiles)
        {
            var identity = item.ItemSpec;
            if (string.IsNullOrWhiteSpace(identity))
                continue;

            var fullPath = Path.IsPathRooted(identity)
                ? Path.GetFullPath(identity)
                : Path.GetFullPath(Path.Combine(projectDir, identity));

            if (!File.Exists(fullPath))
            {
                Log.LogMessage(MessageImportance.Low, "Skipping missing source file: {0}", fullPath);
                continue;
            }

            // Only analyze C# sources
            if (!fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                sources.Add((fullPath, File.ReadAllText(fullPath)));
            }
            catch (Exception ex)
            {
                Log.LogWarning("Could not read source file '{0}': {1}", fullPath, ex.Message);
            }
        }

        if (sources.Count == 0)
        {
            Log.LogWarning("GenerateRoslynWorkflowDiagrams: no readable .cs source files; skipping.");
            return true;
        }

        try
        {
            var results = RoslynWorkflowDiagramExtractor.ExtractFromSources(sources);
            if (results.Count == 0)
            {
                Log.LogWarning(
                    "No workflow types found in project sources. " +
                    "Ensure types are marked with Temporal [Workflow] and have a [WorkflowRun] method. " +
                    "Add TemporalDashboard.WorkflowDiagramming.Roslyn.Build to the project that contains those sources.");
                return true;
            }

            var workflowEntries = new List<WorkflowEntry>();
            var generated = 0;

            foreach (var result in results)
            {
                try
                {
                    foreach (var diagnostic in result.Diagnostics)
                        Log.LogMessage(MessageImportance.Low, "[{0}] {1}", result.DisplayName ?? result.WorkflowTypeName, diagnostic);

                    var shortName = result.DisplayName
                        ?? result.WorkflowTypeName.Split('.').LastOrDefault()
                        ?? result.WorkflowTypeName;
                    var safeName = SanitizeFileName(shortName);
                    var diagramFileName = safeName + extension;
                    var filePath = Path.Combine(outputDir, diagramFileName);
                    File.WriteAllText(filePath, result.Mermaid, System.Text.Encoding.UTF8);

                    workflowEntries.Add(new WorkflowEntry
                    {
                        Name = shortName,
                        DisplayName = result.DisplayName,
                        DiagramFile = diagramFileName
                    });
                    generated++;
                    Log.LogMessage(MessageImportance.Normal, "Generated Roslyn diagram: {0}", filePath);
                }
                catch (Exception ex)
                {
                    Log.LogWarning("Failed to write diagram for workflow '{0}': {1}", result.WorkflowTypeName, ex.Message);
                }
            }

            var metadata = new WorkflowDiagramsMetadata
            {
                AssemblyName = string.IsNullOrWhiteSpace(AssemblyName) ? Path.GetFileName(projectDir.TrimEnd(Path.DirectorySeparatorChar)) : AssemblyName.Trim(),
                AssemblyVersion = string.IsNullOrWhiteSpace(AssemblyVersion) ? "0.0.0.0" : AssemblyVersion.Trim(),
                AssemblyPath = projectDir,
                Language = string.IsNullOrWhiteSpace(Language) ? "C#" : Language.Trim(),
                TargetFramework = TargetFramework?.Trim() ?? string.Empty,
                BuildDateUtc = DateTime.UtcNow.ToString("O"),
                Generator = "TemporalDashboard.WorkflowDiagramming.Roslyn.Build/1.0.0",
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

            Log.LogMessage(MessageImportance.High, "Generated {0} Roslyn workflow diagram(s), metadata, and zip in {1}", generated, outputDir);
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError("Failed to generate Roslyn workflow diagrams: {0}", ex.Message);
            return false;
        }
    }

    private static void CreateZipArchive(string outputDir, string zipPath, string diagramExtension, string metadataPath)
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
