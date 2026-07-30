using TemporalDashboard.WorkflowDiagramming.Models;

namespace TemporalDashboard.WorkflowDiagramming.Roslyn;

/// <summary>
/// Result of extracting a workflow diagram from C# source via Roslyn.
/// </summary>
public sealed class WorkflowExtractionResult
{
    public WorkflowExtractionResult(
        string workflowTypeName,
        string? displayName,
        WorkflowDiagramModel diagram,
        IReadOnlyList<string>? diagnostics = null)
    {
        WorkflowTypeName = workflowTypeName;
        DisplayName = displayName;
        Diagram = diagram;
        Diagnostics = diagnostics ?? Array.Empty<string>();
    }

    /// <summary>Fully qualified or simple name of the workflow type.</summary>
    public string WorkflowTypeName { get; }

    /// <summary>Optional display name (type name by default).</summary>
    public string? DisplayName { get; }

    /// <summary>Structured diagram graph.</summary>
    public WorkflowDiagramModel Diagram { get; }

    /// <summary>Notes about unsupported or approximate constructs.</summary>
    public IReadOnlyList<string> Diagnostics { get; }

    /// <summary>Mermaid flowchart for this diagram.</summary>
    public string Mermaid => MermaidDiagramRenderer.Render(Diagram);
}
