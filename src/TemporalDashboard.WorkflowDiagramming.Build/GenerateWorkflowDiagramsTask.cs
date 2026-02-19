using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using TemporalDashboard.WorkflowDiagramming;
using TemporalDashboard.WorkflowDiagramming.Attributes;
using Temporalio.Workflows;

namespace TemporalDashboard.WorkflowDiagramming.Build;

/// <summary>
/// MSBuild task that loads a built workflow assembly and generates Mermaid diagram files
/// for each type marked with [Workflow] and diagramming attributes.
/// </summary>
public sealed class GenerateWorkflowDiagramsTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Full path to the built assembly (e.g. the workflow project's output DLL).
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory where .mermaid files will be written. Created if it does not exist.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// File extension for generated files (e.g. ".mermaid" or ".md"). Defaults to ".mermaid".
    /// </summary>
    public string FileExtension { get; set; } = ".mermaid";

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

        try
        {
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

                var generated = 0;
                foreach (var workflowType in workflowTypes)
                {
                    try
                    {
                        var mermaid = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
                        var safeName = SanitizeFileName(workflowType.Name);
                        var filePath = Path.Combine(outputDir, safeName + extension);
                        File.WriteAllText(filePath, mermaid, System.Text.Encoding.UTF8);
                        generated++;
                        Log.LogMessage(MessageImportance.Normal, "Generated diagram: {0}", filePath);
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning("Failed to generate diagram for workflow '{0}': {1}", workflowType.Name, ex.Message);
                    }
                }

                Log.LogMessage(MessageImportance.High, "Generated {0} workflow diagram(s) in {1}", generated, outputDir);
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
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
