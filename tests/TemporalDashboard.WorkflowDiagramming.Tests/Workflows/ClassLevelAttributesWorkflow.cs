using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
[WorkflowStep("ClassStep1", "Class Level Step 1", 1)]
[WorkflowStep("ClassStep2", "Class Level Step 2", 2)]
public class ClassLevelAttributesWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowTransition("Start", "ClassStep1")]
    [WorkflowTransition("ClassStep1", "ClassStep2")]
    [WorkflowTransition("ClassStep2", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
