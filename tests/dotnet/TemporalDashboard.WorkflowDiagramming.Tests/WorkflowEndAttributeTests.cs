using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowEndAttributeTests
{
    [Fact]
    public void WorkflowEndAttribute_ConstructorWithIdLabel_SetsProperties()
    {
        var attribute = new WorkflowEndAttribute("End1", "Complete");
        Assert.Equal("End1", attribute.Id);
        Assert.Equal("Complete", attribute.Label);
        Assert.False(attribute.IsFailure);
        Assert.False(attribute.IsSuccess);
    }

    [Fact]
    public void WorkflowEndAttribute_ConstructorWithIsSuccess_SetsSuccessAndFailure()
    {
        var successAttribute = new WorkflowEndAttribute("End1", "Success", true);
        var failureAttribute = new WorkflowEndAttribute("End2", "Failure", false);
        Assert.True(successAttribute.IsSuccess);
        Assert.False(successAttribute.IsFailure);
        Assert.False(failureAttribute.IsSuccess);
        Assert.True(failureAttribute.IsFailure);
    }

    [Fact]
    public void WorkflowEndAttribute_Properties_CanBeSet()
    {
        var attribute = new WorkflowEndAttribute("End1", "Test");
        attribute.IsSuccess = true;
        attribute.IsFailure = false;
        Assert.True(attribute.IsSuccess);
        Assert.False(attribute.IsFailure);
    }
}
