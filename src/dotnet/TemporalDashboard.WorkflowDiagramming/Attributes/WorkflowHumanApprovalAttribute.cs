using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to mark a human-in-the-loop approval step.
/// This is a specialized version of WorkflowStepAttribute for approval steps.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class WorkflowHumanApprovalAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for this approval step
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display label for this approval step
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Order/sequence number for this approval
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Optional description of what requires approval
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional timeout description (e.g., "24 hours")
    /// </summary>
    public string? Timeout { get; set; }

    /// <summary>
    /// Optional role/person who needs to approve (e.g., "Line Manager", "Credit Committee")
    /// </summary>
    public string? ApproverRole { get; set; }

    public WorkflowHumanApprovalAttribute(string id, string label, int order = 0)
    {
        Id = id;
        Label = label;
        Order = order;
    }
}

