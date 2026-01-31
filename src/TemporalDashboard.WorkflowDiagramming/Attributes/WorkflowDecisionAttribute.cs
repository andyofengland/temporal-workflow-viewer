using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to mark a decision point in the workflow.
/// This is a specialized version of WorkflowStepAttribute for decision nodes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class WorkflowDecisionAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for this decision step
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display label/question for this decision
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Order/sequence number for this decision
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Optional description of the decision criteria
    /// </summary>
    public string? Description { get; set; }

    public WorkflowDecisionAttribute(string id, string label, int order = 0)
    {
        Id = id;
        Label = label;
        Order = order;
    }
}

