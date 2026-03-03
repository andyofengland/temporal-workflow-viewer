using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

namespace TemporalDashboard.WorkflowDiagramming;

/// <summary>
/// Generates Mermaid diagrams from workflow classes using reflection and attributes.
/// Resolves attributes by type name so diagrams work when types come from a different AssemblyLoadContext (e.g. MSBuild task).
/// </summary>
public static class WorkflowDiagramGenerator
{
    /// <summary>
    /// Generates a Mermaid flowchart diagram for a workflow type using attributes
    /// </summary>
    public static string GenerateMermaidDiagram(Type workflowType)
    {
        var sb = new StringBuilder();

        // Get workflow diagram metadata (try strong type first, then cross-context by name)
        var workflowDiagram = workflowType.GetCustomAttribute<WorkflowDiagramAttribute>();
        var direction = workflowDiagram?.Direction ?? Attr.Direction(workflowType) ?? "TD";
        sb.AppendLine($"flowchart {direction}");

        // Get the workflow run method (Temporal's [WorkflowRun]; resolve by name for cross-ALC)
        var runMethod = workflowType.GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<WorkflowRunAttribute>() != null)
            ?? workflowType.GetMethods().FirstOrDefault(m => Attr.HasAttribute(m, Attr.WorkflowRun));

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

        // Get start attribute (with cross-context fallback)
        var startAttr = runMethod.GetCustomAttribute<WorkflowStartAttribute>();
        data.StartLabel = startAttr?.Label ?? Attr.StartLabel(runMethod) ?? "Start";

        // Collect all step attributes from the workflow class and methods
        var allMethods = workflowType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        
        // Get steps from attributes on methods (with cross-context fallback via Attr)
        foreach (var method in allMethods)
        {
            // Check for WorkflowStep attributes
            var stepAttrs = method.GetCustomAttributes<WorkflowStepAttribute>().ToList();
            if (stepAttrs.Count > 0)
                foreach (var stepAttr in stepAttrs)
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
            else
                foreach (var a in Attr.GetAttributes(method, Attr.WorkflowStep))
                    data.Steps.Add(new WorkflowStepData
                    {
                        Id = Attr.GetPropString(a, "Id") ?? "",
                        Label = Attr.GetPropString(a, "Label") ?? "",
                        Order = Attr.GetPropInt(a, "Order"),
                        StepType = Attr.GetPropStepType(a),
                        Description = Attr.GetPropString(a, "Description"),
                        IsFailure = Attr.GetPropBool(a, "IsFailure"),
                        IsSuccess = Attr.GetPropBool(a, "IsSuccess"),
                        IsAiPowered = Attr.GetPropBool(a, "IsAiPowered")
                    });

            // Check for WorkflowDecision attributes
            var decisionAttr = method.GetCustomAttribute<WorkflowDecisionAttribute>();
            var decisionAttrObj = decisionAttr != null ? null : Attr.GetAttribute(method, Attr.WorkflowDecision);
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
            else if (decisionAttrObj != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = Attr.GetPropString(decisionAttrObj, "Id") ?? "",
                    Label = Attr.GetPropString(decisionAttrObj, "Label") ?? "",
                    Order = Attr.GetPropInt(decisionAttrObj, "Order"),
                    StepType = WorkflowStepType.Decision,
                    Description = Attr.GetPropString(decisionAttrObj, "Description"),
                    IsAiPowered = false
                });
            }

            // Check for WorkflowHumanApproval attributes
            var approvalAttr = method.GetCustomAttribute<WorkflowHumanApprovalAttribute>();
            var approvalAttrObj = approvalAttr != null ? null : Attr.GetAttribute(method, Attr.WorkflowHumanApproval);
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
            else if (approvalAttrObj != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = Attr.GetPropString(approvalAttrObj, "Id") ?? "",
                    Label = Attr.GetPropString(approvalAttrObj, "Label") ?? "",
                    Order = Attr.GetPropInt(approvalAttrObj, "Order"),
                    StepType = WorkflowStepType.HumanApproval,
                    Description = Attr.GetPropString(approvalAttrObj, "Description"),
                    IsAiPowered = false
                });
            }

            // Check for WorkflowEnd attributes
            var endAttr = method.GetCustomAttribute<WorkflowEndAttribute>();
            var endAttrObj = endAttr != null ? null : Attr.GetAttribute(method, Attr.WorkflowEnd);
            if (endAttr != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = endAttr.Id,
                    Label = endAttr.Label,
                    Order = int.MaxValue,
                    StepType = WorkflowStepType.End,
                    IsFailure = endAttr.IsFailure,
                    IsSuccess = endAttr.IsSuccess,
                    IsAiPowered = false
                });
            }
            else if (endAttrObj != null)
            {
                data.Steps.Add(new WorkflowStepData
                {
                    Id = Attr.GetPropString(endAttrObj, "Id") ?? "",
                    Label = Attr.GetPropString(endAttrObj, "Label") ?? "",
                    Order = int.MaxValue,
                    StepType = WorkflowStepType.End,
                    IsFailure = Attr.GetPropBool(endAttrObj, "IsFailure"),
                    IsSuccess = Attr.GetPropBool(endAttrObj, "IsSuccess"),
                    IsAiPowered = false
                });
            }

            // Check for WorkflowTransition attributes
            var transitionAttrs = method.GetCustomAttributes<WorkflowTransitionAttribute>().ToList();
            var transitionObjs = transitionAttrs.Count == 0 ? Attr.GetAttributes(method, Attr.WorkflowTransition).ToList() : null;
            if (transitionObjs != null)
                foreach (var a in transitionObjs)
                    data.Transitions.Add(new WorkflowTransitionData
                    {
                        From = Attr.GetPropString(a, "From") ?? "",
                        To = Attr.GetPropString(a, "To") ?? "",
                        Label = Attr.GetPropString(a, "Label"),
                        Condition = Attr.GetPropString(a, "Condition"),
                        IsFailurePath = Attr.GetPropBool(a, "IsFailurePath"),
                        IsSuccessPath = Attr.GetPropBool(a, "IsSuccessPath")
                    });
            else
                foreach (var transitionAttr in transitionAttrs)
                    data.Transitions.Add(new WorkflowTransitionData
                    {
                        From = transitionAttr.From,
                        To = transitionAttr.To,
                        Label = transitionAttr.Label,
                        Condition = transitionAttr.Condition,
                        IsFailurePath = transitionAttr.IsFailurePath,
                        IsSuccessPath = transitionAttr.IsSuccessPath
                    });

            // Check for WorkflowBranch attributes
            var branchAttrs = method.GetCustomAttributes<WorkflowBranchAttribute>().ToList();
            var branchObjs = branchAttrs.Count == 0 ? Attr.GetAttributes(method, Attr.WorkflowBranch).ToList() : null;
            if (branchObjs != null)
                foreach (var a in branchObjs)
                    data.Branches.Add(new WorkflowBranchData
                    {
                        DecisionId = Attr.GetPropString(a, "DecisionId") ?? "",
                        Label = Attr.GetPropString(a, "Label") ?? "",
                        TargetStepId = Attr.GetPropString(a, "TargetStepId") ?? "",
                        IsFailurePath = Attr.GetPropBool(a, "IsFailurePath"),
                        IsSuccessPath = Attr.GetPropBool(a, "IsSuccessPath"),
                        IsContinuePath = Attr.GetPropBool(a, "IsContinuePath")
                    });
            else
                foreach (var branchAttr in branchAttrs)
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

        // Also check class-level attributes
        var classStepAttrs = workflowType.GetCustomAttributes<WorkflowStepAttribute>().ToList();
        var classStepObjs = classStepAttrs.Count == 0 ? Attr.GetAttributes(workflowType, Attr.WorkflowStep).ToList() : null;
        if (classStepObjs != null)
            foreach (var a in classStepObjs)
                data.Steps.Add(new WorkflowStepData
                {
                    Id = Attr.GetPropString(a, "Id") ?? "",
                    Label = Attr.GetPropString(a, "Label") ?? "",
                    Order = Attr.GetPropInt(a, "Order"),
                    StepType = Attr.GetPropStepType(a),
                    Description = Attr.GetPropString(a, "Description"),
                    IsFailure = Attr.GetPropBool(a, "IsFailure"),
                    IsSuccess = Attr.GetPropBool(a, "IsSuccess"),
                    IsAiPowered = Attr.GetPropBool(a, "IsAiPowered")
                });
        else
            foreach (var stepAttr in classStepAttrs)
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

    /// <summary>Resolves attributes by full type name so reflection works across AssemblyLoadContext boundaries (e.g. MSBuild task loading user assembly in isolated context).</summary>
    private static class Attr
    {
        public const string WorkflowRun = "Temporalio.Workflows.WorkflowRunAttribute";
        public const string WorkflowDiagram = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowDiagramAttribute";
        public const string WorkflowStart = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowStartAttribute";
        public const string WorkflowStep = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowStepAttribute";
        public const string WorkflowDecision = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowDecisionAttribute";
        public const string WorkflowHumanApproval = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowHumanApprovalAttribute";
        public const string WorkflowEnd = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowEndAttribute";
        public const string WorkflowTransition = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowTransitionAttribute";
        public const string WorkflowBranch = "TemporalDashboard.WorkflowDiagramming.Attributes.WorkflowBranchAttribute";

        public static bool HasAttribute(MemberInfo member, string attributeFullName)
        {
            if (member == null || string.IsNullOrEmpty(attributeFullName)) return false;
            try
            {
                foreach (var a in member.GetCustomAttributes(false))
                    if (a?.GetType().FullName == attributeFullName) return true;
            }
            catch { }
            return false;
        }

        public static object? GetAttribute(MemberInfo member, string attributeFullName)
        {
            if (member == null) return null;
            try
            {
                foreach (var a in member.GetCustomAttributes(false))
                    if (a?.GetType().FullName == attributeFullName) return a;
            }
            catch { }
            return null;
        }

        public static IEnumerable<object> GetAttributes(MemberInfo member, string attributeFullName)
        {
            if (member == null) return [];
            try
            {
                return member.GetCustomAttributes(false)
                    .Where(a => a?.GetType().FullName == attributeFullName)
                    .ToList();
            }
            catch
            {
                return [];
            }
        }

        public static string? Direction(Type type) => GetString(type, WorkflowDiagram, "Direction");
        public static string? StartLabel(MemberInfo method) => GetString(method, WorkflowStart, "Label");

        public static string? GetString(MemberInfo member, string attributeFullName, string propertyName)
        {
            var a = GetAttribute(member, attributeFullName);
            return a == null ? null : GetPropString(a, propertyName);
        }

        public static string? GetPropString(object attr, string propertyName)
        {
            try
            {
                var p = attr.GetType().GetProperty(propertyName);
                return p?.GetValue(attr) as string;
            }
            catch { return null; }
        }

        public static int GetPropInt(object attr, string propertyName, int defaultValue = 0)
        {
            try
            {
                var p = attr.GetType().GetProperty(propertyName);
                var v = p?.GetValue(attr);
                if (v is int i) return i;
                if (v != null && int.TryParse(v.ToString(), out var n)) return n;
            }
            catch { }
            return defaultValue;
        }

        public static bool GetPropBool(object attr, string propertyName, bool defaultValue = false)
        {
            try
            {
                var p = attr.GetType().GetProperty(propertyName);
                var v = p?.GetValue(attr);
                if (v is bool b) return b;
                if (v != null && bool.TryParse(v.ToString(), out var x)) return x;
            }
            catch { }
            return defaultValue;
        }

        public static WorkflowStepType GetPropStepType(object attr, string propertyName = "StepType")
        {
            try
            {
                var p = attr.GetType().GetProperty(propertyName);
                var v = p?.GetValue(attr);
                if (v is WorkflowStepType st) return st;
                if (v is int i) return (WorkflowStepType)i;
                if (v is Enum e) return (WorkflowStepType)Convert.ToInt32(e);
            }
            catch { }
            return WorkflowStepType.Activity;
        }
    }
}
