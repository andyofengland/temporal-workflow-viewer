using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class InvalidWorkflowNoRun
{
    [WorkflowStep("Step1", "Some Step", 1)]
    public void SomeStep() { }
}
