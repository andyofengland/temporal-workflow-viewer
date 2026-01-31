using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowDecisionAttributeTests
{
    [Fact]
    public void WorkflowDecisionAttribute_ConstructorWithIdLabelOrder_SetsProperties()
    {
        var attribute = new WorkflowDecisionAttribute("Decision1", "Is Valid?", 1);
        Assert.Equal("Decision1", attribute.Id);
        Assert.Equal("Is Valid?", attribute.Label);
        Assert.Equal(1, attribute.Order);
        Assert.Null(attribute.Description);
    }

    [Fact]
    public void WorkflowDecisionAttribute_ConstructorWithDefaultOrder_UsesZero()
    {
        var attribute = new WorkflowDecisionAttribute("Decision1", "Test");
        Assert.Equal("Decision1", attribute.Id);
        Assert.Equal("Test", attribute.Label);
        Assert.Equal(0, attribute.Order);
    }

    [Fact]
    public void WorkflowDecisionAttribute_Description_CanBeSet()
    {
        var attribute = new WorkflowDecisionAttribute("Decision1", "Test", 1);
        attribute.Description = "Check if value is valid";
        Assert.Equal("Check if value is valid", attribute.Description);
    }
}
