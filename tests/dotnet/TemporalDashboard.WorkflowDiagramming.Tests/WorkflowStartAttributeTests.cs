using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowStartAttributeTests
{
    [Fact]
    public void WorkflowStartAttribute_DefaultConstructor_SetsDefaultLabel()
    {
        var attribute = new WorkflowStartAttribute();
        Assert.Equal("Start", attribute.Label);
    }

    [Fact]
    public void WorkflowStartAttribute_ConstructorWithLabel_SetsLabel()
    {
        var attribute = new WorkflowStartAttribute("Begin Workflow");
        Assert.Equal("Begin Workflow", attribute.Label);
    }

    [Fact]
    public void WorkflowStartAttribute_Label_CanBeSet()
    {
        var attribute = new WorkflowStartAttribute();
        attribute.Label = "Custom Start";
        Assert.Equal("Custom Start", attribute.Label);
    }
}
