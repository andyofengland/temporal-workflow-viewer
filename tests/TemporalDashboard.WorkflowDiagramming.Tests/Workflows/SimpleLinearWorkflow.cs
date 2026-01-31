using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram(Direction = "TD")]
public class SimpleLinearWorkflow
{
    [WorkflowRun]
    [WorkflowStart("Begin")]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "Process Data", 1)]
    public void ProcessData() { }

    [WorkflowStep("Step2", "Validate Result", 2)]
    public void ValidateResult() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Step2")]
    [WorkflowTransition("Step2", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
