using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowTransitionAttributeTests
{
    [Fact]
    public void WorkflowTransitionAttribute_ConstructorWithFromTo_SetsProperties()
    {
        var attribute = new WorkflowTransitionAttribute("Step1", "Step2");
        Assert.Equal("Step1", attribute.From);
        Assert.Equal("Step2", attribute.To);
        Assert.Null(attribute.Label);
        Assert.Null(attribute.Condition);
        Assert.False(attribute.IsFailurePath);
        Assert.False(attribute.IsSuccessPath);
    }

    [Fact]
    public void WorkflowTransitionAttribute_ConstructorWithLabel_SetsLabel()
    {
        var attribute = new WorkflowTransitionAttribute("Step1", "Step2", "Next");
        Assert.Equal("Step1", attribute.From);
        Assert.Equal("Step2", attribute.To);
        Assert.Equal("Next", attribute.Label);
    }

    [Fact]
    public void WorkflowTransitionAttribute_Properties_CanBeSet()
    {
        var attribute = new WorkflowTransitionAttribute("Step1", "Step2");
        attribute.Label = "Continue";
        attribute.Condition = "if valid";
        attribute.IsFailurePath = true;
        attribute.IsSuccessPath = false;
        Assert.Equal("Continue", attribute.Label);
        Assert.Equal("if valid", attribute.Condition);
        Assert.True(attribute.IsFailurePath);
        Assert.False(attribute.IsSuccessPath);
    }
}
