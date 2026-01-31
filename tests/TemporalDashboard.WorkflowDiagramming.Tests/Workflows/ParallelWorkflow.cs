using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class ParallelWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "Initial Step", 1)]
    public void InitialStep() { }

    [WorkflowStep("Step2A", "Parallel Task A", 2)]
    public void ParallelTaskA() { }

    [WorkflowStep("Step2B", "Parallel Task B", 2)]
    public void ParallelTaskB() { }

    [WorkflowStep("Step3", "Merge Results", 3)]
    public void MergeResults() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Step2A")]
    [WorkflowTransition("Step1", "Step2B")]
    [WorkflowTransition("Step2A", "Step3")]
    [WorkflowTransition("Step2B", "Step3")]
    [WorkflowTransition("Step3", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
