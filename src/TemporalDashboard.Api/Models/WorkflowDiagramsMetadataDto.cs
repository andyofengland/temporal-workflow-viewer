using System.Text.Json.Serialization;

namespace TemporalDashboard.Api.Models;

/// <summary>
/// DTO for workflow-diagrams-metadata.json produced by language extractors / build tasks.
/// Prefers <c>packageName</c>; still accepts legacy <c>assemblyName</c>.
/// </summary>
public sealed class WorkflowDiagramsMetadataDto
{
    [JsonPropertyName("packageName")]
    public string PackageName { get; set; } = string.Empty;

    /// <summary>Legacy field from older .NET packages.</summary>
    [JsonPropertyName("assemblyName")]
    public string? AssemblyName { get; set; }

    [JsonPropertyName("packageVersion")]
    public string PackageVersion { get; set; } = string.Empty;

    [JsonPropertyName("assemblyVersion")]
    public string? AssemblyVersion { get; set; }

    [JsonPropertyName("packagePath")]
    public string? PackagePath { get; set; }

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

    /// <summary>Resolved package identity for storage / UI.</summary>
    [JsonIgnore]
    public string EffectivePackageName =>
        !string.IsNullOrWhiteSpace(PackageName)
            ? PackageName.Trim()
            : (AssemblyName ?? string.Empty).Trim();
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
