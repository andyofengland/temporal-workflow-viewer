using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class LongLabelsWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "This is a very long step label that contains many words and should be properly handled in the diagram generation", 1)]
    public void Step1() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
