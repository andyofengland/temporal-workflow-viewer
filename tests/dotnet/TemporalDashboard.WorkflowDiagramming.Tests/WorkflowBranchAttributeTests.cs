using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowBranchAttributeTests
{
    [Fact]
    public void WorkflowBranchAttribute_ConstructorWithRequiredParams_SetsProperties()
    {
        var attribute = new WorkflowBranchAttribute("Decision1", "Yes", "Step2");
        Assert.Equal("Decision1", attribute.DecisionId);
        Assert.Equal("Yes", attribute.Label);
        Assert.Equal("Step2", attribute.TargetStepId);
        Assert.False(attribute.IsFailurePath);
        Assert.False(attribute.IsSuccessPath);
        Assert.False(attribute.IsContinuePath);
    }

    [Fact]
    public void WorkflowBranchAttribute_Properties_CanBeSet()
    {
        var attribute = new WorkflowBranchAttribute("Decision1", "Yes", "Step2");
        attribute.IsFailurePath = true;
        attribute.IsSuccessPath = false;
        attribute.IsContinuePath = true;
        Assert.True(attribute.IsFailurePath);
        Assert.False(attribute.IsSuccessPath);
        Assert.True(attribute.IsContinuePath);
    }

    [Fact]
    public void WorkflowBranchAttribute_SuccessPath_SetsCorrectly()
    {
        var attribute = new WorkflowBranchAttribute("Decision1", "Approve", "SuccessEnd") { IsSuccessPath = true };
        Assert.True(attribute.IsSuccessPath);
        Assert.False(attribute.IsFailurePath);
    }

    [Fact]
    public void WorkflowBranchAttribute_FailurePath_SetsCorrectly()
    {
        var attribute = new WorkflowBranchAttribute("Decision1", "Reject", "FailureEnd") { IsFailurePath = true };
        Assert.True(attribute.IsFailurePath);
        Assert.False(attribute.IsSuccessPath);
    }
}
