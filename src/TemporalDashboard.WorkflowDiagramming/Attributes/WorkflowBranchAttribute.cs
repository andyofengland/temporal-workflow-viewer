using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to define a branch from a decision point.
/// Apply this alongside WorkflowDecisionAttribute to define the possible outcomes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class WorkflowBranchAttribute : Attribute
{
    /// <summary>
    /// The decision step ID this branch belongs to
    /// </summary>
    public string DecisionId { get; set; }

    /// <summary>
    /// Label for this branch (e.g., "Yes", "No", "Success", "Failure", "Retry")
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Target step ID this branch leads to
    /// </summary>
    public string TargetStepId { get; set; }

    /// <summary>
    /// Whether this branch represents a failure path
    /// </summary>
    public bool IsFailurePath { get; set; }

    /// <summary>
    /// Whether this branch represents a success path
    /// </summary>
    public bool IsSuccessPath { get; set; }

    /// <summary>
    /// Whether this branch continues to the next sequential step
    /// </summary>
    public bool IsContinuePath { get; set; }

    public WorkflowBranchAttribute(string decisionId, string label, string targetStepId)
    {
        DecisionId = decisionId;
        Label = label;
        TargetStepId = targetStepId;
    }
}

