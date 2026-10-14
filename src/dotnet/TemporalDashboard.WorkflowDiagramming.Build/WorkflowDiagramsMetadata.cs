using System.Text.Json.Serialization;

namespace TemporalDashboard.WorkflowDiagramming.Build;

/// <summary>
/// Metadata emitted alongside generated workflow diagrams for sharing and distribution.
/// Serialized as workflow-diagrams-metadata.json and included in the diagrams zip.
/// </summary>
public sealed class WorkflowDiagramsMetadata
{
    /// <summary>Language-agnostic package / project identity (replaces assemblyName).</summary>
    [JsonPropertyName("packageName")]
    public string PackageName { get; set; } = string.Empty;

    /// <summary>Obsolete alias written for older readers; prefer <see cref="PackageName"/>.</summary>
    [JsonPropertyName("assemblyName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AssemblyName
    {
        get => string.IsNullOrEmpty(PackageName) ? null : PackageName;
        set
        {
            if (string.IsNullOrWhiteSpace(PackageName) && !string.IsNullOrWhiteSpace(value))
                PackageName = value.Trim();
        }
    }

    [JsonPropertyName("packageVersion")]
    public string PackageVersion { get; set; } = string.Empty;

    /// <summary>Obsolete alias; prefer <see cref="PackageVersion"/>.</summary>
    [JsonPropertyName("assemblyVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AssemblyVersion
    {
        get => string.IsNullOrEmpty(PackageVersion) ? null : PackageVersion;
        set
        {
            if (string.IsNullOrWhiteSpace(PackageVersion) && !string.IsNullOrWhiteSpace(value))
                PackageVersion = value.Trim();
        }
    }

    [JsonPropertyName("packagePath")]
    public string? PackagePath { get; set; }

    /// <summary>Obsolete alias; prefer <see cref="PackagePath"/>.</summary>
    [JsonPropertyName("assemblyPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AssemblyPath
    {
        get => PackagePath;
        set
        {
            if (string.IsNullOrWhiteSpace(PackagePath) && !string.IsNullOrWhiteSpace(value))
                PackagePath = value;
        }
    }

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
