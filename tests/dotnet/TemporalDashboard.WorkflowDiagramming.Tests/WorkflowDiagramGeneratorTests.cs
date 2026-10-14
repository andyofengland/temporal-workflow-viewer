using TemporalDashboard.WorkflowDiagramming;
using TemporalDashboard.WorkflowDiagramming.Tests;
using TemporalDashboard.WorkflowDiagramming.Tests.Workflows;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowDiagramGeneratorTests
{
    [Fact]
    public void GenerateMermaidDiagram_SimpleLinearWorkflow_GeneratesCorrectDiagram()
    {
        // Arrange
        var workflowType = typeof(SimpleLinearWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("flowchart TD", diagram);
        Assert.Contains("Start([Begin])", diagram);
        Assert.Contains("Step1[\"Process Data\"]", diagram);
        Assert.Contains("Step2[\"Validate Result\"]", diagram);
        Assert.Contains("End([\"Complete\"])", diagram);
        Assert.Contains("Start --> Step1", diagram);
        Assert.Contains("Step1 --> Step2", diagram);
        Assert.Contains("Step2 --> End", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_DecisionWorkflow_GeneratesDecisionNodes()
    {
        // Arrange
        var workflowType = typeof(DecisionWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("flowchart LR", diagram);
        Assert.Contains("Decision1{\"Is Valid?\"}", diagram);
        Assert.Contains("style Decision1", diagram);
        Assert.Contains("Decision1 -->|Yes| Step2", diagram);
        Assert.Contains("Decision1 -->|No| ErrorEnd", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_ApprovalWorkflow_GeneratesHumanApprovalNode()
    {
        // Arrange
        var workflowType = typeof(ApprovalWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("Approval1[\"👤 Manager Approval\"]", diagram);
        Assert.Contains("style Approval1", diagram);
        Assert.Contains("fill:#ffd43b", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_AiPoweredWorkflow_GeneratesAiPoweredNodes()
    {
        // Arrange
        var workflowType = typeof(AiPoweredWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("Step1[\"🤖 Extract Text\"]", diagram);
        Assert.Contains("Step2[\"🤖 Analyze Content\"]", diagram);
        Assert.Contains("style Step1", diagram);
        Assert.Contains("style Step2", diagram);
        Assert.Contains("fill:#e1bee7", diagram);
        Assert.Contains("stroke:#9c27b0", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_ParallelWorkflow_DetectsParallelPatterns()
    {
        // Arrange
        var workflowType = typeof(ParallelWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("Step1 -->", diagram);
        Assert.Contains("Step2A -->", diagram);
        Assert.Contains("Step2B -->", diagram);
        // Check for parallel indicators
        Assert.Contains("Parallel", diagram);
        // Check for parallel styling (dashed lines)
        Assert.Contains("stroke-dasharray", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_ComplexWorkflow_GeneratesCompleteDiagram()
    {
        // Arrange
        var workflowType = typeof(ComplexWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("flowchart TD", diagram);
        Assert.Contains("Workflow Start", diagram);
        Assert.Contains("Decision1", diagram);
        Assert.Contains("Decision2", diagram);
        Assert.Contains("Approval1", diagram);
        Assert.Contains("SuccessEnd", diagram);
        Assert.Contains("FailureEnd", diagram);
        // Check success/failure styling
        Assert.Contains("fill:#51cf66", diagram); // Success color
        Assert.Contains("fill:#ff6b6b", diagram); // Failure color
    }

    [Fact]
    public void GenerateMermaidDiagram_WorkflowWithoutRunMethod_ReturnsErrorDiagram()
    {
        // Arrange
        var workflowType = typeof(InvalidWorkflowNoRun);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("Error[Workflow Run Method Not Found]", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_ClassLevelAttributes_IncludesClassLevelSteps()
    {
        // Arrange
        var workflowType = typeof(ClassLevelAttributesWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("ClassStep1", diagram);
        Assert.Contains("Class Level Step 1", diagram);
        Assert.Contains("ClassStep2", diagram);
        Assert.Contains("Class Level Step 2", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_DefaultDirection_DefaultsToTD()
    {
        // Arrange
        var workflowType = typeof(ApprovalWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.StartsWith("flowchart TD", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_CustomStartLabel_UsesCustomLabel()
    {
        // Arrange
        var workflowType = typeof(SimpleLinearWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("Start([Begin])", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_TransitionLabels_IncludesLabels()
    {
        // Arrange
        var workflowType = typeof(DecisionWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("|Yes|", diagram);
        Assert.Contains("|No|", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_FailurePaths_StylesFailureNodes()
    {
        // Arrange
        var workflowType = typeof(DecisionWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("ErrorEnd", diagram);
        // Failure paths should be styled
        Assert.Contains("fill:#ff6b6b", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_SuccessPaths_StylesSuccessNodes()
    {
        // Arrange
        var workflowType = typeof(ComplexWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("SuccessEnd", diagram);
        Assert.Contains("fill:#51cf66", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_NodeNameSanitization_RemovesSpecialCharacters()
    {
        // Arrange
        var workflowType = typeof(SimpleLinearWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        // Node IDs should not contain special characters
        Assert.DoesNotContain("Step-1", diagram);
        Assert.DoesNotContain("Step_1", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_MultipleEndNodes_IncludesAllEndNodes()
    {
        // Arrange
        var workflowType = typeof(ComplexWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.Contains("SuccessEnd", diagram);
        Assert.Contains("FailureEnd", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_StepsOrderedByOrder_GeneratesInCorrectOrder()
    {
        // Arrange
        var workflowType = typeof(SimpleLinearWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        var step1Index = diagram.IndexOf("Step1");
        var step2Index = diagram.IndexOf("Step2");
        Assert.True(step1Index < step2Index, "Step1 should appear before Step2");
    }

    [Fact]
    public void GenerateMermaidDiagram_EndNodesHaveMaxOrder_AppearAtEnd()
    {
        // Arrange
        var workflowType = typeof(SimpleLinearWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        var endIndex = diagram.IndexOf("End([");
        var step2Index = diagram.IndexOf("Step2");
        Assert.True(step2Index < endIndex, "End should appear after all steps");
    }

    [Fact]
    public void GenerateMermaidDiagram_NoWorkflowDiagramAttribute_UsesDefaultDirection()
    {
        // Arrange
        var workflowType = typeof(ApprovalWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        Assert.StartsWith("flowchart TD", diagram);
    }

    [Fact]
    public void GenerateMermaidDiagram_DecisionBranchesOnly_NoDuplicateTransitions()
    {
        // Arrange
        var workflowType = typeof(DecisionWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        // Decision nodes should only have branch edges, not transition edges
        var decisionTransitions = diagram.Split('\n')
            .Count(line => line.Contains("Decision1 -->") && !line.Contains("|Yes|") && !line.Contains("|No|"));
        Assert.Equal(0, decisionTransitions);
    }

    [Fact]
    public void GenerateMermaidDiagram_AllNodeTypes_GeneratesCorrectShapes()
    {
        // Arrange
        var workflowType = typeof(ComplexWorkflow);

        // Act
        var diagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(workflowType);

        // Assert
        Assert.NotNull(diagram);
        // Start node: rounded rectangle
        Assert.Contains("([", diagram);
        // Activity nodes: square brackets
        Assert.Contains("[\"", diagram);
        // Decision nodes: curly braces
        Assert.Contains("{\"", diagram);
        // End nodes: rounded rectangle
        Assert.Contains("([\"", diagram);
    }
}
