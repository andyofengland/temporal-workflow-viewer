using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class DuplicateStepIdsWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "First Step1", 1)]
    public void Step1A() { }

    [WorkflowStep("Step1", "Second Step1", 2)]
    public void Step1B() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
