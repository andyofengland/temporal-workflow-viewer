using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to mark a workflow class and provide metadata for diagram generation.
/// This should be applied to the workflow class alongside [Workflow].
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class WorkflowDiagramAttribute : Attribute
{
    /// <summary>
    /// Display name for the workflow in diagrams
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of the workflow
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Direction/orientation of the flowchart (TD, LR, TB, BT)
    /// Default is TD (Top Down)
    /// </summary>
    public string Direction { get; set; } = "TD";

    public WorkflowDiagramAttribute()
    {
    }

    public WorkflowDiagramAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

