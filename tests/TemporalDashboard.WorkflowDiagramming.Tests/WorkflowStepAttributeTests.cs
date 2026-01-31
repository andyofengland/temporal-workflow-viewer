using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowStepAttributeTests
{
    [Fact]
    public void WorkflowStepAttribute_ConstructorWithIdLabelOrder_SetsProperties()
    {
        var attribute = new WorkflowStepAttribute("Step1", "Process Data", 1);
        Assert.Equal("Step1", attribute.Id);
        Assert.Equal("Process Data", attribute.Label);
        Assert.Equal(1, attribute.Order);
        Assert.Equal(WorkflowStepType.Activity, attribute.StepType);
        Assert.False(attribute.IsFailure);
        Assert.False(attribute.IsSuccess);
        Assert.False(attribute.IsAiPowered);
    }

    [Fact]
    public void WorkflowStepAttribute_ConstructorWithStepType_SetsStepType()
    {
        var attribute = new WorkflowStepAttribute("Step1", "Decision", WorkflowStepType.Decision, 1);
        Assert.Equal("Step1", attribute.Id);
        Assert.Equal("Decision", attribute.Label);
        Assert.Equal(WorkflowStepType.Decision, attribute.StepType);
        Assert.Equal(1, attribute.Order);
    }

    [Fact]
    public void WorkflowStepAttribute_Properties_CanBeSet()
    {
        var attribute = new WorkflowStepAttribute("Step1", "Test", 1);
        attribute.Description = "Test description";
        attribute.IsFailure = true;
        attribute.IsSuccess = false;
        attribute.IsAiPowered = true;
        attribute.StepType = WorkflowStepType.HumanApproval;
        Assert.Equal("Test description", attribute.Description);
        Assert.True(attribute.IsFailure);
        Assert.False(attribute.IsSuccess);
        Assert.True(attribute.IsAiPowered);
        Assert.Equal(WorkflowStepType.HumanApproval, attribute.StepType);
    }

    [Fact]
    public void WorkflowStepAttribute_AllStepTypes_CanBeSet()
    {
        var attribute = new WorkflowStepAttribute("Step1", "Test", 1);
        attribute.StepType = WorkflowStepType.Activity;
        Assert.Equal(WorkflowStepType.Activity, attribute.StepType);
        attribute.StepType = WorkflowStepType.Decision;
        Assert.Equal(WorkflowStepType.Decision, attribute.StepType);
        attribute.StepType = WorkflowStepType.HumanApproval;
        Assert.Equal(WorkflowStepType.HumanApproval, attribute.StepType);
        attribute.StepType = WorkflowStepType.Start;
        Assert.Equal(WorkflowStepType.Start, attribute.StepType);
        attribute.StepType = WorkflowStepType.End;
        Assert.Equal(WorkflowStepType.End, attribute.StepType);
    }
}
