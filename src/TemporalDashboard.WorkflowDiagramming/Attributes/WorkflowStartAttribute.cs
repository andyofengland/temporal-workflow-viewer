using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to mark the start of a workflow.
/// Apply this to the workflow run method or the first step.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class WorkflowStartAttribute : Attribute
{
    /// <summary>
    /// Display label for the start node (default: "Start")
    /// </summary>
    public string Label { get; set; } = "Start";

    public WorkflowStartAttribute()
    {
    }

    public WorkflowStartAttribute(string label)
    {
        Label = label;
    }
}

