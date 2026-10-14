using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class UnorderedStepsWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step3", "Third Step", 3)]
    public void Step3() { }

    [WorkflowStep("Step1", "First Step", 1)]
    public void Step1() { }

    [WorkflowStep("Step2", "Second Step", 2)]
    public void Step2() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Step2")]
    [WorkflowTransition("Step2", "Step3")]
    [WorkflowTransition("Step3", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
