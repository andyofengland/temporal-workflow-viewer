using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowDiagramAttributeTests
{
    [Fact]
    public void WorkflowDiagramAttribute_DefaultConstructor_SetsDefaultValues()
    {
        var attribute = new WorkflowDiagramAttribute();
        Assert.Null(attribute.DisplayName);
        Assert.Null(attribute.Description);
        Assert.Equal("TD", attribute.Direction);
    }

    [Fact]
    public void WorkflowDiagramAttribute_ConstructorWithDisplayName_SetsDisplayName()
    {
        var attribute = new WorkflowDiagramAttribute("Test Workflow");
        Assert.Equal("Test Workflow", attribute.DisplayName);
        Assert.Null(attribute.Description);
        Assert.Equal("TD", attribute.Direction);
    }

    [Fact]
    public void WorkflowDiagramAttribute_Properties_CanBeSet()
    {
        var attribute = new WorkflowDiagramAttribute();
        attribute.DisplayName = "My Workflow";
        attribute.Description = "A test workflow";
        attribute.Direction = "LR";
        Assert.Equal("My Workflow", attribute.DisplayName);
        Assert.Equal("A test workflow", attribute.Description);
        Assert.Equal("LR", attribute.Direction);
    }

    [Fact]
    public void WorkflowDiagramAttribute_Direction_AcceptsValidValues()
    {
        var attribute = new WorkflowDiagramAttribute();
        attribute.Direction = "TD";
        Assert.Equal("TD", attribute.Direction);
        attribute.Direction = "LR";
        Assert.Equal("LR", attribute.Direction);
        attribute.Direction = "TB";
        Assert.Equal("TB", attribute.Direction);
        attribute.Direction = "BT";
        Assert.Equal("BT", attribute.Direction);
    }
}
