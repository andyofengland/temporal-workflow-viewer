using System.Text;
using System.Text.RegularExpressions;

namespace TemporalDashboard.Web.Services;

/// <summary>
/// Parses a Mermaid flowchart and generates a compilable C# workflow class with diagram annotations.
/// </summary>
public static class MermaidToWorkflowGenerator
{
    private const string DefaultClassName = "GeneratedWorkflow";
    private const string DefaultNamespace = "YourNamespace.YourProject";

    public static string Generate(string mermaid, string? className = null, string? ns = null)
    {
        className = string.IsNullOrWhiteSpace(className) ? DefaultClassName : SanitizeClassName(className);
        ns = string.IsNullOrWhiteSpace(ns) ? DefaultNamespace : ns.Trim();

        if (string.IsNullOrWhiteSpace(mermaid))
            return BuildFile(ns, className, direction: "TD", nodes: new List<ParsedNode>(), edges: new List<ParsedEdge>());

        var (direction, nodes, edges) = ParseMermaid(mermaid);
        return BuildFile(ns, className, direction, nodes, edges);
    }

    public static (bool Success, string? Error) ValidateAndGetError(string mermaid)
    {
        if (string.IsNullOrWhiteSpace(mermaid)) return (false, "Paste a Mermaid flowchart first.");
        try
        {
            ParseMermaid(mermaid);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static (string Direction, List<ParsedNode> Nodes, List<ParsedEdge> Edges) ParseMermaid(string mermaid)
    {
        var lines = mermaid.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("%"))
            .ToList();

        string direction = "TD";
        var nodes = new List<ParsedNode>();
        var edges = new List<ParsedEdge>();
        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (line.StartsWith("style", StringComparison.OrdinalIgnoreCase))
                continue;

            if (Regex.IsMatch(line, @"^\s*flowchart\s+(TD|LR|TB|BT)\s*", RegexOptions.IgnoreCase))
            {
                var m = Regex.Match(line, @"flowchart\s+(TD|LR|TB|BT)", RegexOptions.IgnoreCase);
                if (m.Success) direction = m.Groups[1].Value.ToUpperInvariant();
                continue;
            }

            // Edge: A --> B or A -->|label| B
            var edgeMatch = Regex.Match(line, @"^\s*([A-Za-z0-9_]+)\s*-->\s*(?:\|\s*([^|]*?)\s*\|\s*)?([A-Za-z0-9_]+)\s*$");
            if (edgeMatch.Success)
            {
                edges.Add(new ParsedEdge(
                    edgeMatch.Groups[1].Value.Trim(),
                    edgeMatch.Groups[3].Value.Trim(),
                    edgeMatch.Groups[2].Success ? edgeMatch.Groups[2].Value.Trim() : null));
                nodeIds.Add(edgeMatch.Groups[1].Value.Trim());
                nodeIds.Add(edgeMatch.Groups[3].Value.Trim());
                continue;
            }

            // Node: id["label"] or id([label]) or id{label} or id[label] (with or without quotes)
            var nodeMatch = Regex.Match(line, @"^\s*([A-Za-z0-9_]+)\s*(\[\s*""([^""]*)""\s*\]|\(\s*\[\s*""([^""]*)""\s*\]\s*\)|\(\s*\[\s*([^\]]*)\s*\]\s*\)|\[\s*([^\]]*)\s*\]|\{\s*""([^""]*)""\s*\}|\{\s*([^}]*)\s*\})\s*$");
            if (!nodeMatch.Success)
                nodeMatch = Regex.Match(line, @"^\s*([A-Za-z0-9_]+)\s*\[\s*([^\]]+)\s*\]\s*$"); // id[label] no quotes
            if (nodeMatch.Success)
            {
                var id = nodeMatch.Groups[1].Value.Trim();
                string label = id;
                string shape = "rect";
                if (nodeMatch.Groups.Count >= 3 && nodeMatch.Groups[3].Success) { label = nodeMatch.Groups[3].Value; shape = "rect"; }
                else if (nodeMatch.Groups.Count >= 5 && nodeMatch.Groups[4].Success) { label = nodeMatch.Groups[4].Value; shape = "stadium"; }
                else if (nodeMatch.Groups.Count >= 6 && nodeMatch.Groups[5].Success) { label = nodeMatch.Groups[5].Value.Trim(); shape = "stadium"; }
                else if (nodeMatch.Groups.Count >= 7 && nodeMatch.Groups[6].Success) { label = nodeMatch.Groups[6].Value.Trim(); shape = "rect"; }
                else if (nodeMatch.Groups.Count >= 8 && nodeMatch.Groups[7].Success) { label = nodeMatch.Groups[7].Value; shape = "diamond"; }
                else if (nodeMatch.Groups.Count >= 9 && nodeMatch.Groups[8].Success) { label = nodeMatch.Groups[8].Value.Trim(); shape = "diamond"; }
                else if (nodeMatch.Groups[2].Success && !nodeMatch.Groups[2].Value.Contains("[") && !nodeMatch.Groups[2].Value.TrimStart().StartsWith("(")) { label = nodeMatch.Groups[2].Value.Trim(); shape = "rect"; } // id[label] no quotes
                nodes.Add(new ParsedNode(id, label, shape));
                nodeIds.Add(id);
            }
        }

        // Ensure all nodes referenced in edges exist (with default label = id)
        foreach (var id in nodeIds)
        {
            if (nodes.All(n => !string.Equals(n.Id, id, StringComparison.OrdinalIgnoreCase)))
                nodes.Add(new ParsedNode(id, id, "rect"));
        }

        var hasIncoming = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasOutgoing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in edges)
        {
            hasOutgoing.Add(e.From);
            hasIncoming.Add(e.To);
        }

        // Classify nodes: Start (no incoming or id "Start"), End (no outgoing), Decision (diamond or multiple outgoing with labels)
        var outDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var decisionCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in edges)
        {
            outDegree[e.From] = outDegree.GetValueOrDefault(e.From, 0) + 1;
            if (!string.IsNullOrEmpty(e.Label)) decisionCandidates.Add(e.From);
        }

        foreach (var n in nodes)
        {
            bool noIncoming = !hasIncoming.Contains(n.Id);
            bool noOutgoing = !hasOutgoing.Contains(n.Id);
            if (noIncoming || string.Equals(n.Id, "Start", StringComparison.OrdinalIgnoreCase))
                n.NodeType = "Start";
            else if (noOutgoing)
                n.NodeType = "End";
            else if (n.Shape == "diamond" || (outDegree.GetValueOrDefault(n.Id, 0) > 1 || decisionCandidates.Contains(n.Id)))
                n.NodeType = "Decision";
            else
                n.NodeType = "Step";
        }

        // Only one Start; if multiple "no incoming", pick first or one named Start
        var starts = nodes.Where(n => n.NodeType == "Start").ToList();
        if (starts.Count > 1)
        {
            var primaryStart = starts.FirstOrDefault(s => string.Equals(s.Id, "Start", StringComparison.OrdinalIgnoreCase)) ?? starts[0];
            foreach (var s in starts.Where(s => s != primaryStart))
                s.NodeType = "Step";
        }

        return (direction, nodes, edges);
    }

    private static string BuildFile(string ns, string className, string direction, List<ParsedNode> nodes, List<ParsedEdge> edges)
    {
        var startNode = nodes.FirstOrDefault(n => n.NodeType == "Start");
        var startLabel = startNode?.Label ?? "Start";
        var decisionIds = new HashSet<string>(nodes.Where(n => n.NodeType == "Decision").Select(n => n.Id), StringComparer.OrdinalIgnoreCase);
        var stepNodes = nodes.Where(n => n.NodeType == "Step" || n.NodeType == "Decision").OrderBy(n => n.Id).ToList();
        var endNodes = nodes.Where(n => n.NodeType == "End").ToList();

        var sb = new StringBuilder();
        sb.AppendLine("using Temporalio.Workflows;");
        sb.AppendLine("using TemporalDashboard.WorkflowDiagramming.Attributes;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("[Workflow]");
        sb.AppendLine($"[WorkflowDiagram(DisplayName = \"{Escape(className)}\", Direction = \"{direction}\")]");
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    [WorkflowRun]");
        sb.AppendLine($"    [WorkflowStart(\"{Escape(startLabel)}\")]");
        sb.AppendLine("    public Task<string> RunAsync() => Task.FromResult(\"Done\");");
        sb.AppendLine();

        var usedMethodNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int order = 1;
        foreach (var n in stepNodes)
        {
            if (n.NodeType == "Decision")
            {
                sb.AppendLine($"    [WorkflowDecision(\"{n.Id}\", \"{Escape(n.Label)}\", {order})]");
                sb.AppendLine($"    public bool {MethodName(n.Id, usedMethodNames)}() => true;");
            }
            else
            {
                sb.AppendLine($"    [WorkflowStep(\"{n.Id}\", \"{Escape(n.Label)}\", {order})]");
                sb.AppendLine($"    public void {MethodName(n.Id, usedMethodNames)}() {{ }}");
            }
            sb.AppendLine();
            order++;
        }

        foreach (var n in endNodes)
        {
            bool isSuccess = n.Id.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0
                             || n.Id.IndexOf("complete", StringComparison.OrdinalIgnoreCase) >= 0
                             || n.Id.IndexOf("end", StringComparison.OrdinalIgnoreCase) >= 0 && n.Id.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0;
            sb.AppendLine($"    [WorkflowEnd(\"{n.Id}\", \"{Escape(n.Label)}\", {isSuccess.ToString().ToLowerInvariant()})]");
            sb.AppendLine($"    public void {MethodName(n.Id, usedMethodNames)}() {{ }}");
            sb.AppendLine();
        }

        // Transitions (excluding from decision nodes). Map Mermaid start node id to "Start".
        var startId = startNode?.Id ?? "Start";
        var transitions = edges.Where(e => !decisionIds.Contains(e.From)).ToList();
        if (transitions.Count > 0)
        {
            foreach (var e in transitions)
            {
                var fromId = string.Equals(e.From, startId, StringComparison.OrdinalIgnoreCase) ? "Start" : e.From;
                var label = string.IsNullOrEmpty(e.Label) ? null : $"\"{Escape(e.Label)}\"";
                if (label != null)
                    sb.AppendLine($"    [WorkflowTransition(\"{fromId}\", \"{e.To}\", {label})]");
                else
                    sb.AppendLine($"    [WorkflowTransition(\"{fromId}\", \"{e.To}\")]");
            }
            sb.AppendLine("    public void Transitions() { }");
            sb.AppendLine();
        }

        // Branches (from decision nodes)
        var branches = edges.Where(e => decisionIds.Contains(e.From)).ToList();
        if (branches.Count > 0)
        {
            foreach (var e in branches)
            {
                var failure = e.To.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                              || e.To.IndexOf("reject", StringComparison.OrdinalIgnoreCase) >= 0
                              || (e.Label?.IndexOf("no", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
                if (failure)
                    sb.AppendLine($"    [WorkflowBranch(\"{e.From}\", \"{Escape(e.Label ?? "No")}\", \"{e.To}\", IsFailurePath = true)]");
                else
                    sb.AppendLine($"    [WorkflowBranch(\"{e.From}\", \"{Escape(e.Label ?? "Yes")}\", \"{e.To}\")]");
            }
            sb.AppendLine("    public void DecisionBranches() { }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string MethodName(string id, HashSet<string> used)
    {
        var baseName = Regex.Replace(id, @"[^a-zA-Z0-9_]", "_");
        if (string.IsNullOrEmpty(baseName)) baseName = "Step";
        if (baseName.Length > 0 && char.IsDigit(baseName[0])) baseName = "Step" + baseName;
        var name = baseName;
        var n = 0;
        while (used.Contains(name))
            name = baseName + (++n);
        used.Add(name);
        return name;
    }

    private static string SanitizeClassName(string name)
    {
        var safe = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
        return string.IsNullOrEmpty(safe) ? DefaultClassName : safe;
    }

    private static string Escape(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }

    private class ParsedNode
    {
        public string Id { get; }
        public string Label { get; set; }
        public string Shape { get; }
        public string NodeType { get; set; } = "Step";

        public ParsedNode(string id, string label, string shape)
        {
            Id = id;
            Label = label;
            Shape = shape;
        }
    }

    private class ParsedEdge
    {
        public string From { get; }
        public string To { get; }
        public string? Label { get; }

        public ParsedEdge(string from, string to, string? label)
        {
            From = from;
            To = to;
            Label = label;
        }
    }
}
