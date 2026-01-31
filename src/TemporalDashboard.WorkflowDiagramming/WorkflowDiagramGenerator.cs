using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming;

/// <summary>
/// Generates Mermaid diagrams from workflow classes using reflection and attributes
/// </summary>
public static class WorkflowDiagramGenerator
{
    /// <summary>
    /// Generates a Mermaid flowchart diagram for a workflow type using attributes
    /// </summary>
    public static string GenerateMermaidDiagram(Type workflowType)
    {
        var sb = new StringBuilder();

        // Get workflow diagram metadata
        var workflowDiagram = workflowType.GetCustomAttribute<WorkflowDiagramAttribute>();
        var direction = workflowDiagram?.Direction ?? "TD";
        sb.AppendLine($"flowchart {direction}");

        // Get the workflow run method
        var runMethod = workflowType.GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<WorkflowRunAttribute>() != null);

        if (runMethod == null)
        {
            return "flowchart TD\n    Error[Workflow Run Method Not Found]";
        }

        // Analyze the workflow using attributes
        var workflowData = AnalyzeWorkflowFromAttributes(workflowType, runMethod);
        
        // Generate nodes
        var nodeMap = new Dictionary<string, string>(); // Maps step ID to Mermaid node ID

        // Generate start node
        var startLabel = workflowData.StartLabel ?? "Start";
        var startNodeId = "Start";
        sb.AppendLine($"    {startNodeId}([{startLabel}])");
        nodeMap["Start"] = startNodeId;

        // Generate all step nodes
        foreach (var step in workflowData.Steps.OrderBy(s => s.Order))
        {
            var mermaidNodeId = SanitizeNodeName(step.Id);
            nodeMap[step.Id] = mermaidNodeId;

            // Generate node based on step type
            switch (step.StepType)
            {
                case WorkflowStepType.Activity:
                    // Add AI indicator if this is an AI-powered activity
                    var activityLabel = step.IsAiPowered ? $"🤖 {step.Label}" : step.Label;
                    sb.AppendLine($"    {mermaidNodeId}[\"{activityLabel}\"]");
                    // Style AI-powered activities with a distinct color (purple/magenta)
                    if (step.IsAiPowered)
                    {
                        sb.AppendLine($"    style {mermaidNodeId} fill:#e1bee7,stroke:#9c27b0,stroke-width:3px");
                    }
                    break;
                case WorkflowStepType.Decision:
                    sb.AppendLine($"    {mermaidNodeId}{{\"{step.Label}\"}}");
                    sb.AppendLine($"    style {mermaidNodeId} fill:#e3f2fd,stroke:#1976d2,stroke-width:2px");
                    break;
                case WorkflowStepType.HumanApproval:
                    sb.AppendLine($"    {mermaidNodeId}[\"👤 {step.Label}\"]");
                    sb.AppendLine($"    style {mermaidNodeId} fill:#ffd43b,stroke:#333,stroke-width:2px");
                    break;
                case WorkflowStepType.Start:
                    sb.AppendLine($"    {mermaidNodeId}([{step.Label}])");
                    break;
                case WorkflowStepType.End:
                    sb.AppendLine($"    {mermaidNodeId}([\"{step.Label}\"])");
                    if (step.IsFailure)
                    {
                        sb.AppendLine($"    style {mermaidNodeId} fill:#ff6b6b,stroke:#333,stroke-width:2px");
                    }
                    else if (step.IsSuccess)
                    {
                        sb.AppendLine($"    style {mermaidNodeId} fill:#51cf66,stroke:#333,stroke-width:2px");
                    }
                    break;
            }
        }

        // Identify decision node IDs to avoid duplicate edges
        var decisionNodeIds = new HashSet<string>(
            workflowData.Steps
                .Where(s => s.StepType == WorkflowStepType.Decision)
                .Select(s => s.Id)
        );

        // Detect parallel execution patterns: multiple steps converging on the same target
        var parallelGroups = workflowData.Transitions
            .Where(t => !decisionNodeIds.Contains(t.From))
            .GroupBy(t => t.To)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(t => t.From).ToList());

        // Detect parallel start: multiple steps that start from the same source
        var parallelStartGroups = workflowData.Transitions
            .Where(t => !decisionNodeIds.Contains(t.From))
            .GroupBy(t => t.From)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(t => t.To).ToList());

        // Generate edges/transitions (but skip transitions FROM decision nodes - those are handled by branches)
        foreach (var transition in workflowData.Transitions)
        {
            // Skip transitions that originate from decision nodes - branches handle those
            if (decisionNodeIds.Contains(transition.From))
            {
                continue;
            }

            if (nodeMap.TryGetValue(transition.From, out var fromNode) && 
                nodeMap.TryGetValue(transition.To, out var toNode))
            {
                var edgeLabel = !string.IsNullOrEmpty(transition.Label) 
                    ? $"|{transition.Label}|" 
                    : "";
                
                // Check if this is part of a parallel execution group (converging)
                if (parallelGroups.TryGetValue(transition.To, out var parallelSources) && 
                    parallelSources.Contains(transition.From))
                {
                    // Add parallel indicator to edge label
                    if (string.IsNullOrEmpty(edgeLabel))
                    {
                        edgeLabel = "|Parallel|";
                    }
                    else
                    {
                        edgeLabel = edgeLabel.Replace("|", "") + " (Parallel)";
                        edgeLabel = $"|{edgeLabel}|";
                    }
                }
                // Check if this is part of a parallel start (diverging)
                else if (parallelStartGroups.TryGetValue(transition.From, out var parallelTargets) && 
                         parallelTargets.Contains(transition.To))
                {
                    // Add parallel indicator to edge label
                    if (string.IsNullOrEmpty(edgeLabel))
                    {
                        edgeLabel = "|Parallel|";
                    }
                    else
                    {
                        edgeLabel = edgeLabel.Replace("|", "") + " (Parallel)";
                        edgeLabel = $"|{edgeLabel}|";
                    }
                }
                
                sb.AppendLine($"    {fromNode} -->{edgeLabel} {toNode}");
            }
        }
        
        // Add visual styling for parallel convergence points (join nodes)
        foreach (var parallelGroup in parallelGroups)
        {
            if (nodeMap.TryGetValue(parallelGroup.Key, out var joinNode))
            {
                // Style the join node to indicate parallel convergence
                sb.AppendLine($"    style {joinNode} stroke-dasharray: 5 5,stroke-width:3px");
            }
        }
        
        // Add visual styling for parallel start points (fork nodes)
        foreach (var parallelStartGroup in parallelStartGroups)
        {
            if (nodeMap.TryGetValue(parallelStartGroup.Key, out var forkNode))
            {
                // Style the fork node to indicate parallel divergence
                sb.AppendLine($"    style {forkNode} stroke-dasharray: 5 5,stroke-width:3px");
            }
        }

        // Generate branches from decisions (these are the primary edges for decision nodes)
        foreach (var branch in workflowData.Branches)
        {
            if (nodeMap.TryGetValue(branch.DecisionId, out var decisionNode) && 
                nodeMap.TryGetValue(branch.TargetStepId, out var targetNode))
            {
                var edgeLabel = !string.IsNullOrEmpty(branch.Label) 
                    ? $"|{branch.Label}|" 
                    : "";
                sb.AppendLine($"    {decisionNode} -->{edgeLabel} {targetNode}");

                // Style failure paths
                if (branch.IsFailurePath)
                {
                    sb.AppendLine($"    style {targetNode} fill:#ff6b6b,stroke:#333,stroke-width:2px");
                }
            }
        }

        return sb.ToString();
    }

    private static string SanitizeNodeName(string name)
    {
        // Remove special characters for Mermaid node IDs
        return Regex.Replace(name, @"[^a-zA-Z0-9]", "");
    }

    /// <summary>
    /// Analyzes a workflow type and its run method to extract workflow diagram data from attributes
    /// </summary>
    private static WorkflowDiagramData AnalyzeWorkflowFromAttributes(Type workflowType, MethodInfo runMethod)
    {
        var data = new WorkflowDiagramData();

        // Get start attribute
        var startAttr = runMethod.GetCustomAttribute<WorkflowStartAttribute>();
        data.StartLabel = startAttr?.Label ?? "Start";

        // Collect all step attributes from the workflow class and methods
        var allMethods = workflowType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        
        // Get steps from attributes on methods
        foreach (var method in allMethods)
        {
            // Check for WorkflowStep attributes
            var stepAttrs = method.GetCustomAttributes<WorkflowStepAttribute>();
            foreach (var stepAttr in stepAttrs)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = stepAttr.Id,
                    Label = stepAttr.Label,
                    Order = stepAttr.Order,
                    StepType = stepAttr.StepType,
                    Description = stepAttr.Description,
                    IsFailure = stepAttr.IsFailure,
                    IsSuccess = stepAttr.IsSuccess,
                    IsAiPowered = stepAttr.IsAiPowered
                });
            }

            // Check for WorkflowDecision attributes
            var decisionAttr = method.GetCustomAttribute<WorkflowDecisionAttribute>();
            if (decisionAttr != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = decisionAttr.Id,
                    Label = decisionAttr.Label,
                    Order = decisionAttr.Order,
                    StepType = WorkflowStepType.Decision,
                    Description = decisionAttr.Description,
                    IsAiPowered = false
                });
            }

            // Check for WorkflowHumanApproval attributes
            var approvalAttr = method.GetCustomAttribute<WorkflowHumanApprovalAttribute>();
            if (approvalAttr != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = approvalAttr.Id,
                    Label = approvalAttr.Label,
                    Order = approvalAttr.Order,
                    StepType = WorkflowStepType.HumanApproval,
                    Description = approvalAttr.Description,
                    IsAiPowered = false
                });
            }

            // Check for WorkflowEnd attributes
            var endAttr = method.GetCustomAttribute<WorkflowEndAttribute>();
            if (endAttr != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = endAttr.Id,
                    Label = endAttr.Label,
                    Order = int.MaxValue, // End steps should be last
                    StepType = WorkflowStepType.End,
                    IsFailure = endAttr.IsFailure,
                    IsSuccess = endAttr.IsSuccess,
                    IsAiPowered = false
                });
            }

            // Check for WorkflowTransition attributes
            var transitionAttrs = method.GetCustomAttributes<WorkflowTransitionAttribute>();
            foreach (var transitionAttr in transitionAttrs)
            {
                data.Transitions.Add(new WorkflowTransitionData
                {
                    From = transitionAttr.From,
                    To = transitionAttr.To,
                    Label = transitionAttr.Label,
                    Condition = transitionAttr.Condition,
                    IsFailurePath = transitionAttr.IsFailurePath,
                    IsSuccessPath = transitionAttr.IsSuccessPath
                });
            }

            // Check for WorkflowBranch attributes
            var branchAttrs = method.GetCustomAttributes<WorkflowBranchAttribute>();
            foreach (var branchAttr in branchAttrs)
            {
                data.Branches.Add(new WorkflowBranchData
                {
                    DecisionId = branchAttr.DecisionId,
                    Label = branchAttr.Label,
                    TargetStepId = branchAttr.TargetStepId,
                    IsFailurePath = branchAttr.IsFailurePath,
                    IsSuccessPath = branchAttr.IsSuccessPath,
                    IsContinuePath = branchAttr.IsContinuePath
                });
            }
        }

        // Also check class-level attributes
        var classStepAttrs = workflowType.GetCustomAttributes<WorkflowStepAttribute>();
        foreach (var stepAttr in classStepAttrs)
        {
            data.Steps.Add(new WorkflowStepData
            {
                Id = stepAttr.Id,
                Label = stepAttr.Label,
                Order = stepAttr.Order,
                StepType = stepAttr.StepType,
                Description = stepAttr.Description,
                IsFailure = stepAttr.IsFailure,
                IsSuccess = stepAttr.IsSuccess,
                IsAiPowered = stepAttr.IsAiPowered
            });
        }

        return data;
    }

    /// <summary>
    /// Internal data structure for workflow diagram generation
    /// </summary>
    private class WorkflowDiagramData
    {
        public string StartLabel { get; set; } = "Start";
        public List<WorkflowStepData> Steps { get; set; } = new();
        public List<WorkflowTransitionData> Transitions { get; set; } = new();
        public List<WorkflowBranchData> Branches { get; set; } = new();
    }

    private class WorkflowStepData
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public int Order { get; set; }
        public WorkflowStepType StepType { get; set; }
        public string? Description { get; set; }
        public bool IsFailure { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsAiPowered { get; set; }
    }

    private class WorkflowTransitionData
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string? Label { get; set; }
        public string? Condition { get; set; }
        public bool IsFailurePath { get; set; }
        public bool IsSuccessPath { get; set; }
    }

    private class WorkflowBranchData
    {
        public string DecisionId { get; set; } = "";
        public string Label { get; set; } = "";
        public string TargetStepId { get; set; } = "";
        public bool IsFailurePath { get; set; }
        public bool IsSuccessPath { get; set; }
        public bool IsContinuePath { get; set; }
    }
}


