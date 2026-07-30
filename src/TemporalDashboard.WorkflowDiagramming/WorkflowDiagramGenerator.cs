using System.Reflection;
using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;
using TemporalDashboard.WorkflowDiagramming.Models;

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
        var runMethod = workflowType.GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<WorkflowRunAttribute>() != null)
            ?? workflowType.GetMethods().FirstOrDefault(m => Attr.HasAttribute(m, Attr.WorkflowRun));

        if (runMethod == null)
        {
            return "flowchart TD\n    Error[Workflow Run Method Not Found]";
        }

        var model = AnalyzeWorkflowFromAttributes(workflowType, runMethod);
        return MermaidDiagramRenderer.Render(model);
    }

    /// <summary>
    /// Builds a <see cref="WorkflowDiagramModel"/> from diagramming attributes on the type.
    /// </summary>
    public static WorkflowDiagramModel AnalyzeWorkflow(Type workflowType)
    {
        var runMethod = workflowType.GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<WorkflowRunAttribute>() != null)
            ?? workflowType.GetMethods().FirstOrDefault(m => Attr.HasAttribute(m, Attr.WorkflowRun));

        if (runMethod == null)
        {
            return new WorkflowDiagramModel();
        }

        return AnalyzeWorkflowFromAttributes(workflowType, runMethod);
    }

    private static WorkflowDiagramModel AnalyzeWorkflowFromAttributes(Type workflowType, MethodInfo runMethod)
    {
        var data = new WorkflowDiagramModel();

        var workflowDiagram = workflowType.GetCustomAttribute<WorkflowDiagramAttribute>();
        data.Direction = workflowDiagram?.Direction ?? Attr.Direction(workflowType) ?? "TD";

        var startAttr = runMethod.GetCustomAttribute<WorkflowStartAttribute>();
        data.StartLabel = startAttr?.Label ?? Attr.StartLabel(runMethod) ?? "Start";

        var allMethods = workflowType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var method in allMethods)
        {
            var stepAttrs = method.GetCustomAttributes<WorkflowStepAttribute>().ToList();
            if (stepAttrs.Count > 0)
                foreach (var stepAttr in stepAttrs)
                    data.Steps.Add(new WorkflowStepModel
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
                    data.Steps.Add(new WorkflowStepModel
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

            var decisionAttr = method.GetCustomAttribute<WorkflowDecisionAttribute>();
            var decisionAttrObj = decisionAttr != null ? null : Attr.GetAttribute(method, Attr.WorkflowDecision);
            if (decisionAttr != null)
            {
                data.Steps.Add(new WorkflowStepModel
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
                data.Steps.Add(new WorkflowStepModel
                {
                    Id = Attr.GetPropString(decisionAttrObj, "Id") ?? "",
                    Label = Attr.GetPropString(decisionAttrObj, "Label") ?? "",
                    Order = Attr.GetPropInt(decisionAttrObj, "Order"),
                    StepType = WorkflowStepType.Decision,
                    Description = Attr.GetPropString(decisionAttrObj, "Description"),
                    IsAiPowered = false
                });
            }

            var approvalAttr = method.GetCustomAttribute<WorkflowHumanApprovalAttribute>();
            var approvalAttrObj = approvalAttr != null ? null : Attr.GetAttribute(method, Attr.WorkflowHumanApproval);
            if (approvalAttr != null)
            {
                data.Steps.Add(new WorkflowStepModel
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
                data.Steps.Add(new WorkflowStepModel
                {
                    Id = Attr.GetPropString(approvalAttrObj, "Id") ?? "",
                    Label = Attr.GetPropString(approvalAttrObj, "Label") ?? "",
                    Order = Attr.GetPropInt(approvalAttrObj, "Order"),
                    StepType = WorkflowStepType.HumanApproval,
                    Description = Attr.GetPropString(approvalAttrObj, "Description"),
                    IsAiPowered = false
                });
            }

            var endAttr = method.GetCustomAttribute<WorkflowEndAttribute>();
            var endAttrObj = endAttr != null ? null : Attr.GetAttribute(method, Attr.WorkflowEnd);
            if (endAttr != null)
            {
                data.Steps.Add(new WorkflowStepModel
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
                data.Steps.Add(new WorkflowStepModel
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

            var transitionAttrs = method.GetCustomAttributes<WorkflowTransitionAttribute>().ToList();
            var transitionObjs = transitionAttrs.Count == 0 ? Attr.GetAttributes(method, Attr.WorkflowTransition).ToList() : null;
            if (transitionObjs != null)
                foreach (var a in transitionObjs)
                    data.Transitions.Add(new WorkflowTransitionModel
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
                    data.Transitions.Add(new WorkflowTransitionModel
                    {
                        From = transitionAttr.From,
                        To = transitionAttr.To,
                        Label = transitionAttr.Label,
                        Condition = transitionAttr.Condition,
                        IsFailurePath = transitionAttr.IsFailurePath,
                        IsSuccessPath = transitionAttr.IsSuccessPath
                    });

            var branchAttrs = method.GetCustomAttributes<WorkflowBranchAttribute>().ToList();
            var branchObjs = branchAttrs.Count == 0 ? Attr.GetAttributes(method, Attr.WorkflowBranch).ToList() : null;
            if (branchObjs != null)
                foreach (var a in branchObjs)
                    data.Branches.Add(new WorkflowBranchModel
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
                    data.Branches.Add(new WorkflowBranchModel
                    {
                        DecisionId = branchAttr.DecisionId,
                        Label = branchAttr.Label,
                        TargetStepId = branchAttr.TargetStepId,
                        IsFailurePath = branchAttr.IsFailurePath,
                        IsSuccessPath = branchAttr.IsSuccessPath,
                        IsContinuePath = branchAttr.IsContinuePath
                    });
        }

        var classStepAttrs = workflowType.GetCustomAttributes<WorkflowStepAttribute>().ToList();
        var classStepObjs = classStepAttrs.Count == 0 ? Attr.GetAttributes(workflowType, Attr.WorkflowStep).ToList() : null;
        if (classStepObjs != null)
            foreach (var a in classStepObjs)
                data.Steps.Add(new WorkflowStepModel
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
                data.Steps.Add(new WorkflowStepModel
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
