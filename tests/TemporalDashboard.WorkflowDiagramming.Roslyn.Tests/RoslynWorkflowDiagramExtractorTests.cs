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
    public void Early_return_guard_does_not_make_end_a_parallel_hub()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class GuardWorkflow
            {
                [WorkflowRun]
                public async Task<string> RunAsync(string? input)
                {
                    if (input == null)
                        return "fail";

                    await Workflow.ExecuteActivityAsync(
                        (Acts a) => a.Work(),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });

                    return "ok";
                }
            }
            """;

        var mermaid = RoslynWorkflowDiagramExtractor.ExtractFromSource(source)[0].Mermaid;
        Assert.DoesNotContain("End -->|Parallel|", mermaid);
        Assert.DoesNotContain("Decision{", mermaid);
        Assert.Contains("Work", mermaid);
        Assert.Contains("Start -->", mermaid);
    }

    [Fact]
    public void WorkflowStep_helpers_collapse_to_single_nodes()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;
            using TemporalDashboard.WorkflowDiagramming.Attributes;

            namespace Sample;

            [Workflow]
            public class AttributedHelpersWorkflow
            {
                [WorkflowRun]
                public async Task RunAsync()
                {
                    await StepOneAsync();
                    await StepTwoAsync();
                }

                [WorkflowStep("step-one", "Get ID&V URL from API", 1)]
                private async Task StepOneAsync()
                {
                    await TrackActivityAsync("inner", () => Workflow.ExecuteActivityAsync(
                        (Acts a) => a.GetUrl(),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) }));
                    await SendProgressIfConfigured();
                }

                [WorkflowStep("step-two", "Communicate URL", 2)]
                private async Task StepTwoAsync()
                {
                    await Workflow.ExecuteActivityAsync(
                        (Acts a) => a.Send(),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
                }

                private async Task TrackActivityAsync(string name, Func<Task> work) => await work();
                private Task SendProgressIfConfigured() => Task.CompletedTask;
            }
            """;

        var mermaid = RoslynWorkflowDiagramExtractor.ExtractFromSource(source)[0].Mermaid;
        Assert.Contains("Get ID&V URL from API", mermaid);
        Assert.Contains("Communicate URL", mermaid);
        Assert.DoesNotContain("GetUrl", mermaid);
        Assert.DoesNotContain("SendAsync", mermaid);
        Assert.DoesNotContain("Decision{", mermaid);
    }

    [Fact]
    public void TrackActivity_and_AwaitAsync_helpers_map_to_coarse_steps()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;

            namespace Sample;

            [Workflow]
            public class HelperWorkflow
            {
                private bool _done;

                [WorkflowRun]
                public async Task RunAsync()
                {
                    await TrackActivityAsync("Get ID&V URL", () => Workflow.ExecuteActivityAsync(
                        (Acts a) => a.GetIdVUrlAsync(),
                        new() { StartToCloseTimeout = TimeSpan.FromMinutes(1) }));
                    await SendProgressIfConfigured();
                    await AwaitAsync(
                        new RunWait { Kind = "signal", Name = "idv-callback", Description = "Waiting for the customer to complete identity verification." },
                        () => _done);
                }

                private async Task TrackActivityAsync(string name, Func<Task> work) => await work();
                private Task SendProgressIfConfigured() =>
                    Workflow.ExecuteActivityAsync((Acts a) => a.SendAsync(), new() { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
                private async Task AwaitAsync(RunWait wait, Func<bool> condition) =>
                    await Workflow.WaitConditionAsync(condition);
            }

            public class RunWait
            {
                public string Kind { get; set; }
                public string Name { get; set; }
                public string Description { get; set; }
            }
            """;

        var mermaid = RoslynWorkflowDiagramExtractor.ExtractFromSource(source)[0].Mermaid;
        Assert.Contains("Get ID&V URL", mermaid);
        Assert.Contains("Waiting for the customer to complete identity verification.", mermaid);
        Assert.DoesNotContain("SendAsync", mermaid);
        Assert.DoesNotContain("GetIdVUrlAsync", mermaid);
    }

    [Fact]
    public void WhenAll_with_task_locals_fans_out_from_shared_predecessors()
    {
        const string source = """
            using System.Threading.Tasks;
            using Temporalio.Workflows;
            using TemporalDashboard.WorkflowDiagramming.Attributes;

            namespace Sample;

            [Workflow]
            public class DeferredParallelWorkflow
            {
                private bool _aml;

                [WorkflowRun]
                public async Task RunAsync()
                {
                    await SetupAsync();
                    var termsTask = RunTermsAsync();
                    var amlTask = AwaitAsync(
                        new RunWait { Description = "Waiting for AML approval." },
                        () => _aml);
                    await Task.WhenAll(termsTask, amlTask);
                    await FinishAsync();
                }

                [WorkflowStep("setup", "Setup", 1)]
                private Task SetupAsync() => Task.CompletedTask;

                [WorkflowStep("terms-branch", "Terms and Conditions", 2)]
                private Task RunTermsAsync() => Task.CompletedTask;

                private Task AwaitAsync(RunWait wait, Func<bool> condition) => Task.CompletedTask;
                private Task FinishAsync() => Task.CompletedTask;

                private sealed class RunWait { public string Description { get; set; } = ""; }
            }
            """;

        var mermaid = RoslynWorkflowDiagramExtractor.ExtractFromSource(source)[0].Mermaid;
        Assert.Contains("Setup", mermaid);
        Assert.Contains("Terms and Conditions", mermaid);
        Assert.Contains("Waiting for AML approval.", mermaid);
        Assert.Contains("WhenAll", mermaid);
        Assert.Contains("Parallel", mermaid);
        Assert.DoesNotContain("Start -->|Parallel| End", mermaid);
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
