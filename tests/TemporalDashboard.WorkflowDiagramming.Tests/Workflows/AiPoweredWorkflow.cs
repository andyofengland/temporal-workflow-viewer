using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

[Workflow]
[WorkflowDiagram]
public class AiPoweredWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() => Task.FromResult("Done");

    [WorkflowStep("Step1", "Extract Text", 1, IsAiPowered = true)]
    public void ExtractText() { }

    [WorkflowStep("Step2", "Analyze Content", 2, IsAiPowered = true)]
    public void AnalyzeContent() { }

    [WorkflowStep("Step3", "Save Results", 3)]
    public void SaveResults() { }

    [WorkflowTransition("Start", "Step1")]
    [WorkflowTransition("Step1", "Step2")]
    [WorkflowTransition("Step2", "Step3")]
    [WorkflowTransition("Step3", "End")]
    public void Transitions() { }

    [WorkflowEnd("End", "Complete", true)]
    public void End() { }
}
