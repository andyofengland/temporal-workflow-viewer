namespace TemporalDashboard.Api.Models;

public class WorkflowInfo
{
    public string DllName { get; set; } = string.Empty;
    public string DllPath { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    /// <summary>Display name from [WorkflowDiagram(DisplayName = "...")], or empty if not set.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
}
