using TemporalDashboard.WorkflowDiagramming.Roslyn;

namespace TemporalDashboard.WorkflowDiagramming.Roslyn.Tests;

public class RoslynWorkflowDiagramExtractorTests
{
    [Fact]
    public void Linear_activities_produce_start_to_end_chain()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class OrderWorkflow
            {
                [WorkflowRun]
                public async Task<string> RunAsync(string orderId)
                {
                    await Workflow.ExecuteActivityAsync(
                        (OrderActivities a) => a.Validate(orderId),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });

                    await Workflow.ExecuteActivityAsync(
                        (OrderActivities a) => a.Charge(orderId),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });

                    return "ok";
                }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Single(results);

        var mermaid = results[0].Mermaid;
        Assert.Contains("flowchart TD", mermaid);
        Assert.Contains("Start([Start])", mermaid);
        Assert.Contains("Validate", mermaid);
        Assert.Contains("Charge", mermaid);
        Assert.Contains("End([\"Complete\"])", mermaid);
        Assert.Contains("Start -->", mermaid);
        Assert.Contains("Validate -->", mermaid);
        Assert.Contains("Charge -->", mermaid);
        Assert.Contains("--> End", mermaid);
    }

    [Fact]
    public void If_decision_produces_diamond_and_branches()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class ReviewWorkflow
            {
                [WorkflowRun]
                public async Task RunAsync(bool needsReview)
                {
                    await Workflow.ExecuteActivityAsync(
                        (Acts a) => a.Prepare(),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });

                    if (needsReview)
                    {
                        await Workflow.ExecuteActivityAsync(
                            (Acts a) => a.RequestReview(),
                            new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
                    }
                    else
                    {
                        await Workflow.ExecuteActivityAsync(
                            (Acts a) => a.AutoApprove(),
                            new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
                    }
                }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Single(results);

        var mermaid = results[0].Mermaid;
        Assert.Contains("Prepare", mermaid);
        Assert.Contains("{\"needsReview\"}", mermaid);
        Assert.Contains("RequestReview", mermaid);
        Assert.Contains("AutoApprove", mermaid);
        Assert.Contains("-->|Yes|", mermaid);
        Assert.Contains("-->|No|", mermaid);
        Assert.Contains("fill:#e3f2fd", mermaid);
    }

    [Fact]
    public void WhenAll_produces_parallel_fan_out_and_join()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class ParallelWorkflow
            {
                [WorkflowRun]
                public async Task RunAsync()
                {
                    await Task.WhenAll(
                        Workflow.ExecuteActivityAsync(
                            (Acts a) => a.Ship(),
                            new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) }),
                        Workflow.ExecuteActivityAsync(
                            (Acts a) => a.Notify(),
                            new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) }));
                }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Single(results);

        var mermaid = results[0].Mermaid;
        Assert.Contains("Ship", mermaid);
        Assert.Contains("Notify", mermaid);
        Assert.Contains("WhenAll", mermaid);
        Assert.Contains("Parallel", mermaid);
        Assert.Contains("stroke-dasharray", mermaid);
    }

    [Fact]
    public void WaitCondition_maps_to_human_approval_style_node()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class ApprovalWorkflow
            {
                private bool _approved;

                [WorkflowRun]
                public async Task RunAsync()
                {
                    await Workflow.WaitConditionAsync(() => _approved);
                    await Workflow.ExecuteActivityAsync(
                        (Acts a) => a.Finalize(),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
                }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Single(results);

        var mermaid = results[0].Mermaid;
        Assert.Contains("👤 Wait", mermaid);
        Assert.Contains("fill:#ffd43b", mermaid);
        Assert.Contains("Finalize", mermaid);
    }

    [Fact]
    public void Child_workflow_and_delay_are_detected()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class ParentWorkflow
            {
                [WorkflowRun]
                public async Task RunAsync()
                {
                    await Workflow.ExecuteChildWorkflowAsync(
                        (ChildWorkflow c) => c.RunAsync(),
                        new() { Id = "child-1" });
                    await Workflow.DelayAsync(TimeSpan.FromHours(1));
                }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Single(results);

        var mermaid = results[0].Mermaid;
        Assert.Contains("ChildWorkflow", mermaid);
        Assert.Contains("Delay", mermaid);
    }

    [Fact]
    public void Non_workflow_types_are_ignored()
    {
        const string source = """
            public class NotAWorkflow
            {
                public void Run() { }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Empty(results);
    }

    [Fact]
    public void Multiple_workflows_in_one_source_are_all_extracted()
    {
        const string source = """
            using Temporalio.Workflows;

            [Workflow]
            public class A
            {
                [WorkflowRun]
                public async Task RunAsync()
                {
                    await Workflow.ExecuteActivityAsync("ActA", new object[] { }, new());
                }
            }

            [Workflow]
            public class B
            {
                [WorkflowRun]
                public async Task RunAsync()
                {
                    await Workflow.ExecuteActivityAsync("ActB", new object[] { }, new());
                }
            }
            """;

        var results = RoslynWorkflowDiagramExtractor.ExtractFromSource(source);
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Mermaid.Contains("ActA"));
        Assert.Contains(results, r => r.Mermaid.Contains("ActB"));
    }
}
