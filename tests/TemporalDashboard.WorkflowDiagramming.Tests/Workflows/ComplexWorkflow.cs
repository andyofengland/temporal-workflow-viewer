using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram(Direction = "TD")]
public class ComplexWorkflow
{
    [WorkflowRun]
    [WorkflowStart("Workflow Start")]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "Initialize", 1)]
    public void Initialize() { }

    [WorkflowDecision("Decision1", "Check Condition", 2)]
    public void CheckCondition() { }

    [WorkflowBranch("Decision1", "True", "Step2")]
    [WorkflowBranch("Decision1", "False", "Step3")]
    public void Decision1Branches() { }

    [WorkflowStep("Step2", "Path A", 3)]
    public void PathA() { }

    [WorkflowStep("Step3", "Path B", 3)]
    public void PathB() { }

    [WorkflowHumanApproval("Approval1", "Review Decision", 4)]
    public void ReviewDecision() { }

    [WorkflowDecision("Decision2", "Final Check", 5)]
    public void FinalCheck() { }

    [WorkflowBranch("Decision2", "Approve", "SuccessEnd", IsSuccessPath = true)]
    [WorkflowBranch("Decision2", "Reject", "FailureEnd", IsFailurePath = true)]
    public void Decision2Branches() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Decision1")]
    [WorkflowTransition("Step2", "Approval1")]
    [WorkflowTransition("Step3", "Approval1")]
    [WorkflowTransition("Approval1", "Decision2")]
    public void Transitions() { }

    [WorkflowEnd("SuccessEnd", "Success", true)]
    public void SuccessEnd() { }

    [WorkflowEnd("FailureEnd", "Failure", false)]
    public void FailureEnd() { }
}
