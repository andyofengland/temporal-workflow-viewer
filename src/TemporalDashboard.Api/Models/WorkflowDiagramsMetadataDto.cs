using System.Text.Json.Serialization;

namespace TemporalDashboard.Api.Models;

/// <summary>
/// DTO for workflow-diagrams-metadata.json produced by the build task.
/// </summary>
public sealed class WorkflowDiagramsMetadataDto
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
    public List<WorkflowEntryDto> Workflows { get; set; } = new();
}

public sealed class WorkflowEntryDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("diagramFile")]
    public string DiagramFile { get; set; } = string.Empty;
}
