using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming.Tests;

public class WorkflowHumanApprovalAttributeTests
{
    [Fact]
    public void WorkflowHumanApprovalAttribute_ConstructorWithIdLabelOrder_SetsProperties()
    {
        var attribute = new WorkflowHumanApprovalAttribute("Approval1", "Manager Approval", 1);
        Assert.Equal("Approval1", attribute.Id);
        Assert.Equal("Manager Approval", attribute.Label);
        Assert.Equal(1, attribute.Order);
        Assert.Null(attribute.Description);
        Assert.Null(attribute.Timeout);
        Assert.Null(attribute.ApproverRole);
    }

    [Fact]
    public void WorkflowHumanApprovalAttribute_ConstructorWithDefaultOrder_UsesZero()
    {
        var attribute = new WorkflowHumanApprovalAttribute("Approval1", "Test");
        Assert.Equal("Approval1", attribute.Id);
        Assert.Equal("Test", attribute.Label);
        Assert.Equal(0, attribute.Order);
    }

    [Fact]
    public void WorkflowHumanApprovalAttribute_Properties_CanBeSet()
    {
        var attribute = new WorkflowHumanApprovalAttribute("Approval1", "Test", 1);
        attribute.Description = "Requires manager approval";
        attribute.Timeout = "24 hours";
        attribute.ApproverRole = "Line Manager";
        Assert.Equal("Requires manager approval", attribute.Description);
        Assert.Equal("24 hours", attribute.Timeout);
        Assert.Equal("Line Manager", attribute.ApproverRole);
    }

    [Fact]
    public void WorkflowHumanApprovalAttribute_AllOptionalProperties_CanBeNull()
    {
        var attribute = new WorkflowHumanApprovalAttribute("Approval1", "Test");
        Assert.Null(attribute.Description);
        Assert.Null(attribute.Timeout);
        Assert.Null(attribute.ApproverRole);
    }
}
