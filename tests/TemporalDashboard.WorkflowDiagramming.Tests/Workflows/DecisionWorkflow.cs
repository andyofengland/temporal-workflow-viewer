using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram(Direction = "LR")]
public class DecisionWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "Initial Step", 1)]
    public void InitialStep() { }

    [WorkflowDecision("Decision1", "Is Valid?", 2)]
    public void CheckValidity() { }

    [WorkflowBranch("Decision1", "Yes", "Step2")]
    [WorkflowBranch("Decision1", "No", "ErrorEnd", IsFailurePath = true)]
    public void DecisionBranches() { }

    [WorkflowStep("Step2", "Process Valid Data", 3)]
    public void ProcessValid() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Decision1")]
    public void Transitions() { }

    [WorkflowEnd("End", "Success", true)]
    public void SuccessEnd() { }

    [WorkflowEnd("ErrorEnd", "Error", false)]
    public void ErrorEnd() { }
}
