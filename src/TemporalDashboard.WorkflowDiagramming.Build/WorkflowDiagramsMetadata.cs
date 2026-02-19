using System.Text.Json.Serialization;

namespace TemporalDashboard.WorkflowDiagramming.Build;

/// <summary>
/// Metadata emitted alongside generated workflow diagrams for sharing and distribution.
/// Serialized as workflow-diagrams-metadata.json and included in the diagrams zip.
/// </summary>
public sealed class WorkflowDiagramsMetadata
{
    /// <summary>Assembly name (e.g. MyWorkflows).</summary>
    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>Assembly version (e.g. 1.0.0.0).</summary>
    [JsonPropertyName("assemblyVersion")]
    public string AssemblyVersion { get; set; } = string.Empty;

    /// <summary>Full path to the source assembly at build time.</summary>
    [JsonPropertyName("assemblyPath")]
    public string? AssemblyPath { get; set; }

    /// <summary>Language (e.g. C#).</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "C#";

    /// <summary>Target framework (e.g. net10.0).</summary>
    [JsonPropertyName("targetFramework")]
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>Build date (UTC, ISO 8601).</summary>
    [JsonPropertyName("buildDateUtc")]
    public string BuildDateUtc { get; set; } = string.Empty;

    /// <summary>Generator name and version.</summary>
    [JsonPropertyName("generator")]
    public string Generator { get; set; } = string.Empty;

    /// <summary>List of workflows with diagram file names.</summary>
    [JsonPropertyName("workflows")]
    public List<WorkflowEntry> Workflows { get; set; } = new();
}

/// <summary>Single workflow entry in the metadata.</summary>
public sealed class WorkflowEntry
{
    /// <summary>Workflow type name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Display name from [WorkflowDiagram], if set.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Generated diagram file name (e.g. MyWorkflow.mermaid).</summary>
    [JsonPropertyName("diagramFile")]
    public string DiagramFile { get; set; } = string.Empty;
}
