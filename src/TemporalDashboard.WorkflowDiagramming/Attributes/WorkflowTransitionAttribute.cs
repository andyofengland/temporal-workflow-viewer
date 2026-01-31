using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to define a transition/edge between workflow steps.
/// Apply this to methods or code blocks to define how control flows from one step to another.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class WorkflowTransitionAttribute : Attribute
{
    /// <summary>
    /// Source step ID (where the transition starts)
    /// </summary>
    public string From { get; set; }

    /// <summary>
    /// Target step ID (where the transition ends)
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// Optional label for the edge (e.g., "Yes", "No", "Success", "Failure")
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Optional condition description (for decision branches)
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Whether this transition represents a failure path
    /// </summary>
    public bool IsFailurePath { get; set; }

    /// <summary>
    /// Whether this transition represents a success path
    /// </summary>
    public bool IsSuccessPath { get; set; }

    public WorkflowTransitionAttribute(string from, string to)
    {
        From = from;
        To = to;
    }

    public WorkflowTransitionAttribute(string from, string to, string label)
    {
        From = from;
        To = to;
        Label = label;
    }
}

