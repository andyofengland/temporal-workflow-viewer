using System.Text.Json.Serialization;

namespace TemporalDashboard.WorkflowDiagramming.Roslyn.Build;

/// <summary>
/// Metadata emitted alongside generated workflow diagrams for sharing and distribution.
/// Serialized as workflow-diagrams-metadata.json and included in the diagrams zip.
/// </summary>
public sealed class WorkflowDiagramsMetadata
{
    [JsonPropertyName("assemblyName")]
    public string AssemblyName { get; set; } = string.Empty;

    [JsonPropertyName("assemblyVersion")]
    public string AssemblyVersion { get; set; } = string.Empty;

    [JsonPropertyName("assemblyPath")]
    public string? AssemblyPath { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "C#";

    [JsonPropertyName("targetFramework")]
    public string TargetFramework { get; set; } = string.Empty;

    [JsonPropertyName("buildDateUtc")]
    public string BuildDateUtc { get; set; } = string.Empty;

    [JsonPropertyName("generator")]
    public string Generator { get; set; } = string.Empty;

    [JsonPropertyName("workflows")]
    public List<WorkflowEntry> Workflows { get; set; } = new();
}

/// <summary>Single workflow entry in the metadata.</summary>
public sealed class WorkflowEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("diagramFile")]
    public string DiagramFile { get; set; } = string.Empty;
}
