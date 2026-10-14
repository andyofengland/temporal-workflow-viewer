using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to mark the end of a workflow or a completion point.
/// Apply this to return statements or final steps.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class WorkflowEndAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for this end step
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display label for the end node
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Whether this end represents a failure/completion
    /// </summary>
    public bool IsFailure { get; set; }

    /// <summary>
    /// Whether this end represents a success/completion
    /// </summary>
    public bool IsSuccess { get; set; }

    public WorkflowEndAttribute(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public WorkflowEndAttribute(string id, string label, bool isSuccess)
    {
        Id = id;
        Label = label;
        IsSuccess = isSuccess;
        IsFailure = !isSuccess;
    }
}

