using System;

namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Attribute to mark a workflow step/activity in the workflow execution.
/// Apply this to code blocks, method calls, or activity invocations to define steps in the diagram.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class WorkflowStepAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for this step (used for transitions)
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display label for this step in the diagram
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Order/sequence number for this step (lower numbers execute first)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Type of step (Activity, Decision, HumanApproval, Start, End)
    /// </summary>
    public WorkflowStepType StepType { get; set; } = WorkflowStepType.Activity;

    /// <summary>
    /// Optional description of what this step does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this step represents a failure/error state
    /// </summary>
    public bool IsFailure { get; set; }

    /// <summary>
    /// Whether this step represents a success/completion state
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Whether this step uses AI/ML capabilities (e.g., LLM, OCR, vision models)
    /// </summary>
    public bool IsAiPowered { get; set; }

    public WorkflowStepAttribute(string id, string label, int order = 0)
    {
        Id = id;
        Label = label;
        Order = order;
    }

    public WorkflowStepAttribute(string id, string label, WorkflowStepType stepType, int order = 0)
    {
        Id = id;
        Label = label;
        StepType = stepType;
        Order = order;
    }
}

