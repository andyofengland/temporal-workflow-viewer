using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class EmptyLabelsWorkflow
{
    [WorkflowRun]
    [WorkflowStart("")]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "", 1)]
    public void Step1() { }

    [WorkflowTransition("Start", "Step1", "")]
    [WorkflowTransition("Step1", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "", true)]
    public void End() { }
}
