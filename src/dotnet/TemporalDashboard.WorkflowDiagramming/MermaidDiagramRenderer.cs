using System.Text;
using System.Text.RegularExpressions;
using TemporalDashboard.WorkflowDiagramming.Attributes;
using TemporalDashboard.WorkflowDiagramming.Models;

namespace TemporalDashboard.WorkflowDiagramming;

/// <summary>
/// Renders a <see cref="WorkflowDiagramModel"/> to Mermaid flowchart text.
/// Shared by attribute-based and Roslyn-based extractors.
/// </summary>
public static class MermaidDiagramRenderer
{
    /// <summary>
    /// Renders the diagram model as a Mermaid flowchart string.
    /// </summary>
    public static string Render(WorkflowDiagramModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sb = new StringBuilder();
        var direction = string.IsNullOrWhiteSpace(model.Direction) ? "TD" : model.Direction;
        sb.AppendLine($"flowchart {direction}");

        var nodeMap = new Dictionary<string, string>();

        var startLabel = string.IsNullOrWhiteSpace(model.StartLabel) ? "Start" : model.StartLabel;
        var startNodeId = "Start";
        sb.AppendLine($"    {startNodeId}([{startLabel}])");
        nodeMap["Start"] = startNodeId;

        foreach (var step in model.Steps.OrderBy(s => s.Order))
        {
            var mermaidNodeId = SanitizeNodeName(step.Id);
            nodeMap[step.Id] = mermaidNodeId;

            switch (step.StepType)
            {
                case WorkflowStepType.Activity:
                    var activityLabel = step.IsAiPowered ? $"🤖 {step.Label}" : step.Label;
                    sb.AppendLine($"    {mermaidNodeId}[\"{activityLabel}\"]");
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

        var decisionNodeIds = new HashSet<string>(
            model.Steps
                .Where(s => s.StepType == WorkflowStepType.Decision)
                .Select(s => s.Id));

        var parallelGroups = model.Transitions
            .Where(t => !decisionNodeIds.Contains(t.From))
            .GroupBy(t => t.To)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(t => t.From).ToList());

        var parallelStartGroups = model.Transitions
            .Where(t => !decisionNodeIds.Contains(t.From))
            .GroupBy(t => t.From)
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(t => t.To).ToList());

        foreach (var transition in model.Transitions)
        {
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

                if (parallelGroups.TryGetValue(transition.To, out var parallelSources) &&
                    parallelSources.Contains(transition.From))
                {
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
                else if (parallelStartGroups.TryGetValue(transition.From, out var parallelTargets) &&
                         parallelTargets.Contains(transition.To))
                {
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

        foreach (var parallelGroup in parallelGroups)
        {
            if (nodeMap.TryGetValue(parallelGroup.Key, out var joinNode))
            {
                sb.AppendLine($"    style {joinNode} stroke-dasharray: 5 5,stroke-width:3px");
            }
        }

        foreach (var parallelStartGroup in parallelStartGroups)
        {
            if (nodeMap.TryGetValue(parallelStartGroup.Key, out var forkNode))
            {
                sb.AppendLine($"    style {forkNode} stroke-dasharray: 5 5,stroke-width:3px");
            }
        }

        foreach (var branch in model.Branches)
        {
            if (nodeMap.TryGetValue(branch.DecisionId, out var decisionNode) &&
                nodeMap.TryGetValue(branch.TargetStepId, out var targetNode))
            {
                var edgeLabel = !string.IsNullOrEmpty(branch.Label)
                    ? $"|{branch.Label}|"
                    : "";
                sb.AppendLine($"    {decisionNode} -->{edgeLabel} {targetNode}");

                if (branch.IsFailurePath)
                {
                    sb.AppendLine($"    style {targetNode} fill:#ff6b6b,stroke:#333,stroke-width:2px");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>Removes non-alphanumeric characters for Mermaid node IDs.</summary>
    public static string SanitizeNodeName(string name) =>
        Regex.Replace(name, @"[^a-zA-Z0-9]", "");
}
