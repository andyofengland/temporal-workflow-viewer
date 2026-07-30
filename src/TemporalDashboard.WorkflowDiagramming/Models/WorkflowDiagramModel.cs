using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Models;

/// <summary>
/// Structured workflow diagram graph shared by attribute and Roslyn extractors.
/// </summary>
public sealed class WorkflowDiagramModel
{
    /// <summary>Label for the synthetic Start node.</summary>
    public string StartLabel { get; set; } = "Start";

    /// <summary>Mermaid flowchart direction (e.g. TD, LR).</summary>
    public string Direction { get; set; } = "TD";

    /// <summary>Diagram steps (activities, decisions, approvals, ends).</summary>
    public List<WorkflowStepModel> Steps { get; set; } = new();

    /// <summary>Edges between non-decision nodes.</summary>
    public List<WorkflowTransitionModel> Transitions { get; set; } = new();

    /// <summary>Labeled edges from decision nodes.</summary>
    public List<WorkflowBranchModel> Branches { get; set; } = new();
}

/// <summary>A single node in the workflow diagram.</summary>
public sealed class WorkflowStepModel
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public int Order { get; set; }
    public WorkflowStepType StepType { get; set; }
    public string? Description { get; set; }
    public bool IsFailure { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsAiPowered { get; set; }
}

/// <summary>An edge between two steps (not originating from a decision).</summary>
public sealed class WorkflowTransitionModel
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string? Label { get; set; }
    public string? Condition { get; set; }
    public bool IsFailurePath { get; set; }
    public bool IsSuccessPath { get; set; }
}

/// <summary>A labeled branch from a decision node to a target step.</summary>
public sealed class WorkflowBranchModel
{
    public string DecisionId { get; set; } = "";
    public string Label { get; set; } = "";
    public string TargetStepId { get; set; } = "";
    public bool IsFailurePath { get; set; }
    public bool IsSuccessPath { get; set; }
    public bool IsContinuePath { get; set; }
}
