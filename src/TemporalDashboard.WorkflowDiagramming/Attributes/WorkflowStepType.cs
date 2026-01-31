namespace TemporalDashboard.WorkflowDiagramming.Attributes;

/// <summary>
/// Types of workflow steps for diagram generation
/// </summary>
public enum WorkflowStepType
{
    /// <summary>
    /// Regular activity/operation
    /// </summary>
    Activity,

    /// <summary>
    /// Decision point (diamond shape)
    /// </summary>
    Decision,

    /// <summary>
    /// Human-in-the-loop approval step
    /// </summary>
    HumanApproval,

    /// <summary>
    /// Start node (rounded rectangle)
    /// </summary>
    Start,

    /// <summary>
    /// End node (rounded rectangle)
    /// </summary>
    End
}
