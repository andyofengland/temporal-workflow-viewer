using TemporalDashboard.WorkflowDiagramming;
using TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class EdgeCaseTests
{
    [Fact]
    public void GenerateMermaidDiagram_EmptyLabels_HandlesGracefully()
    {
        var workflowType = typeof(EmptyLabelsWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("flowchart TD", diagram);
        Assert.Contains("Start", diagram);
        Assert.Contains("Step1", diagram);
        Assert.Contains("End", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_SpecialCharacters_SanitizesNodeNames()
    {
        var workflowType = typeof(SpecialCharactersWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("Step1WithSpecial", diagram);
        Assert.Contains("Step & Label", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_LongLabels_HandlesCorrectly()
    {
        var workflowType = typeof(LongLabelsWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("very long step label", diagram);
        Assert.Contains("Step1", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_UnorderedSteps_OrdersCorrectly()
    {
        var workflowType = typeof(UnorderedStepsWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        var step1Index = diagram.IndexOf("First Step");
        var step2Index = diagram.IndexOf("Second Step");
        var step3Index = diagram.IndexOf("Third Step");
        Assert.True(step1Index < step2Index, "Step1 should appear before Step2");
        Assert.True(step2Index < step3Index, "Step2 should appear before Step3");
    }

    [Fact]
    public void GenerateMermaidDiagram_DuplicateStepIds_HandlesGracefully()
    {
        var workflowType = typeof(DuplicateStepIdsWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("flowchart TD", diagram);
        Assert.Contains("Step1", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_NonWorkflowType_HandlesGracefully()
    {
        var nonWorkflowType = typeof(string);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(nonWorkflowType);
        Assert.NotNull(diagram);
        Assert.Contains("Error", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_WorkflowWithNoSteps_GeneratesMinimalDiagram()
    {
        var workflowType = typeof(SimpleLinearWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("Start", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_WorkflowWithNoTransitions_GeneratesNodesOnly()
    {
        var workflowType = typeof(SimpleLinearWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("Step1", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_MultipleWorkflowDiagramAttributes_UsesFirst()
    {
        var workflowType = typeof(SimpleLinearWorkflow);
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);
        Assert.NotNull(diagram);
        Assert.Contains("flowchart TD", diagram);
    }
}
