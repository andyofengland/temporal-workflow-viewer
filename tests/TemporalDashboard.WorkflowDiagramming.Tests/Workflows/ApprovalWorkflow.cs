using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class ApprovalWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "Prepare Document", 1)]
    public void PrepareDocument() { }

    [WorkflowHumanApproval("Approval1", "Manager Approval", 2)]
    public void ManagerApproval() { }

    [WorkflowStep("Step2", "Finalize", 3)]
    public void FinalizeStep() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Approval1")]
    [WorkflowTransition("Approval1", "Step2")]
    [WorkflowTransition("Step2", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
