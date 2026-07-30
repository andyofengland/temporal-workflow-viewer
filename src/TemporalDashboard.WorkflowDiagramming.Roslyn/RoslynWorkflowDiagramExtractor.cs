using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TemporalDashboard.WorkflowDiagramming.Attributes;
using TemporalDashboard.WorkflowDiagramming.Models;

namespace TemporalDashboard.WorkflowDiagramming.Roslyn;

/// <summary>
/// Extracts workflow diagrams from Temporal .NET C# source using Roslyn (no diagramming attributes required).
/// </summary>
public static class RoslynWorkflowDiagramExtractor
{
    /// <summary>Parse a single compilation unit and extract workflow diagrams.</summary>
    public static IReadOnlyList<WorkflowExtractionResult> ExtractFromSource(string csharpSource, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(csharpSource);
        var path = filePath ?? "Workflow.cs";
        return ExtractFromSources([(path, csharpSource)]);
    }

    /// <summary>Parse multiple source files and extract workflow diagrams.</summary>
    public static IReadOnlyList<WorkflowExtractionResult> ExtractFromSources(IEnumerable<(string path, string text)> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var trees = sources
            .Select(s => CSharpSyntaxTree.ParseText(s.text, path: s.path))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "WorkflowDiagramExtraction",
            trees,
            references: [],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return ExtractFromCompilation(compilation);
    }

    /// <summary>Extract workflow diagrams from an existing compilation.</summary>
    public static IReadOnlyList<WorkflowExtractionResult> ExtractFromCompilation(Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        var results = new List<WorkflowExtractionResult>();

        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (!HasAttribute(typeDecl.AttributeLists, "Workflow"))
                {
                    continue;
                }

                var runMethod = typeDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => HasAttribute(m.AttributeLists, "WorkflowRun"));

                if (runMethod?.Body == null && runMethod?.ExpressionBody == null)
                {
                    results.Add(new WorkflowExtractionResult(
                        GetTypeName(typeDecl),
                        typeDecl.Identifier.Text,
                        new WorkflowDiagramModel
                        {
                            StartLabel = "Start",
                            Direction = "TD",
                            Steps =
                            [
                                new WorkflowStepModel
                                {
                                    Id = "End",
                                    Label = "End",
                                    Order = int.MaxValue,
                                    StepType = WorkflowStepType.End,
                                    IsSuccess = true
                                }
                            ],
                            Transitions =
                            [
                                new WorkflowTransitionModel { From = "Start", To = "End" }
                            ]
                        },
                        ["WorkflowRun method body not found or empty; produced Start → End skeleton."]));
                    continue;
                }

                var builder = new WorkflowGraphBuilder(typeDecl);
                builder.AnalyzeRunMethod(runMethod);
                results.Add(builder.ToResult());
            }
        }

        return results;
    }

    internal static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string shortName)
    {
        foreach (var list in attributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                var name = attr.Name.ToString();
                // Workflow, WorkflowAttribute, Temporalio.Workflows.Workflow, etc.
                var simple = name.Contains('.') ? name[(name.LastIndexOf('.') + 1)..] : name;
                if (simple is "WorkflowAttribute" or "WorkflowRunAttribute")
                {
                    simple = simple[..^"Attribute".Length];
                }

                if (string.Equals(simple, shortName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetTypeName(TypeDeclarationSyntax typeDecl)
    {
        var ns = typeDecl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var name = typeDecl.Identifier.Text;
        if (ns == null)
        {
            return name;
        }

        return $"{ns.Name}.{name}";
    }
}

/// <summary>Builds a <see cref="WorkflowDiagramModel"/> by walking a WorkflowRun method body.</summary>
internal sealed class WorkflowGraphBuilder
{
    private readonly TypeDeclarationSyntax _typeDecl;
    private readonly WorkflowDiagramModel _model = new() { StartLabel = "Start", Direction = "TD" };
    private readonly List<string> _diagnostics = new();
    private readonly HashSet<string> _usedIds = new(StringComparer.Ordinal);
    private int _order;
    private int _anonCounter;

    public WorkflowGraphBuilder(TypeDeclarationSyntax typeDecl)
    {
        _typeDecl = typeDecl;
    }

    public void AnalyzeRunMethod(MethodDeclarationSyntax runMethod)
    {
        IReadOnlyList<string> exits;
        if (runMethod.ExpressionBody != null)
        {
            exits = AnalyzeExpression(runMethod.ExpressionBody.Expression, ["Start"]);
        }
        else
        {
            exits = AnalyzeBlock(runMethod.Body!, ["Start"]);
        }

        // Ensure End node and connect remaining exits
        EnsureEndNode();
        foreach (var from in exits.Distinct())
        {
            if (from == "End")
            {
                continue;
            }

            AddTransition(from, "End");
        }

        // If somehow no transitions from Start, connect Start → End
        if (_model.Transitions.Count == 0 && _model.Branches.Count == 0)
        {
            AddTransition("Start", "End");
        }
    }

    public WorkflowExtractionResult ToResult()
    {
        var name = _typeDecl.Identifier.Text;
        var ns = _typeDecl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var fullName = ns == null ? name : $"{ns.Name}.{name}";
        return new WorkflowExtractionResult(fullName, name, _model, _diagnostics);
    }

    private IReadOnlyList<string> AnalyzeBlock(BlockSyntax block, IReadOnlyList<string> predecessors)
    {
        var current = predecessors;
        foreach (var statement in block.Statements)
        {
            current = AnalyzeStatement(statement, current);
        }

        return current;
    }

    private IReadOnlyList<string> AnalyzeStatement(StatementSyntax statement, IReadOnlyList<string> predecessors)
    {
        switch (statement)
        {
            case BlockSyntax nested:
                return AnalyzeBlock(nested, predecessors);

            case LocalDeclarationStatementSyntax local:
                return AnalyzeExpressionList(
                    local.Declaration.Variables.Select(v => v.Initializer?.Value).Where(e => e != null)!,
                    predecessors);

            case ExpressionStatementSyntax exprStmt:
                return AnalyzeExpression(exprStmt.Expression, predecessors);

            case ReturnStatementSyntax ret:
                if (ret.Expression != null)
                {
                    var after = AnalyzeExpression(ret.Expression, predecessors);
                    EnsureEndNode();
                    foreach (var from in after)
                    {
                        AddTransition(from, "End");
                    }

                    return ["End"];
                }

                EnsureEndNode();
                foreach (var from in predecessors)
                {
                    AddTransition(from, "End");
                }

                return ["End"];

            case IfStatementSyntax ifStmt:
                return AnalyzeIf(ifStmt, predecessors);

            case SwitchStatementSyntax switchStmt:
                return AnalyzeSwitch(switchStmt, predecessors);

            case WhileStatementSyntax:
            case ForStatementSyntax:
            case ForEachStatementSyntax:
            case DoStatementSyntax:
                _diagnostics.Add($"Loop at line {statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1} approximated as sequential body (cycles not modeled in v1).");
                if (statement is WhileStatementSyntax w)
                {
                    return AnalyzeStatement(w.Statement, predecessors);
                }

                if (statement is ForStatementSyntax f)
                {
                    return AnalyzeStatement(f.Statement, predecessors);
                }

                if (statement is ForEachStatementSyntax fe)
                {
                    return AnalyzeStatement(fe.Statement, predecessors);
                }

                if (statement is DoStatementSyntax d)
                {
                    return AnalyzeStatement(d.Statement, predecessors);
                }

                return predecessors;

            case TryStatementSyntax tryStmt:
                var afterTry = AnalyzeBlock(tryStmt.Block, predecessors);
                // Ignore catch/finally for v1 skeleton
                if (tryStmt.Catches.Count > 0 || tryStmt.Finally != null)
                {
                    _diagnostics.Add($"try/catch/finally at line {statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: only try body is modeled in v1.");
                }

                return afterTry;

            default:
                // Ignore unsupported statements in v1
                return predecessors;
        }
    }

    private IReadOnlyList<string> AnalyzeExpressionList(IEnumerable<ExpressionSyntax?> expressions, IReadOnlyList<string> predecessors)
    {
        var current = predecessors;
        foreach (var expr in expressions)
        {
            if (expr != null)
            {
                current = AnalyzeExpression(expr, current);
            }
        }

        return current;
    }

    private IReadOnlyList<string> AnalyzeExpression(ExpressionSyntax expression, IReadOnlyList<string> predecessors)
    {
        expression = Unwrap(expression);

        // Assignment: analyze RHS
        if (expression is AssignmentExpressionSyntax assign)
        {
            return AnalyzeExpression(assign.Right, predecessors);
        }

        // await expr
        if (expression is AwaitExpressionSyntax awaitExpr)
        {
            return AnalyzeExpression(awaitExpr.Expression, predecessors);
        }

        // Task.WhenAll / WhenAny
        if (TryGetWhenAllOrAny(expression, out var parallelArgs, out var isWhenAny))
        {
            return AnalyzeParallel(parallelArgs, predecessors, isWhenAny);
        }

        // Temporal Workflow.* calls
        if (TryMatchTemporalCall(expression, out var kind, out var label))
        {
            var stepType = kind == TemporalCallKind.Wait
                ? WorkflowStepType.HumanApproval
                : WorkflowStepType.Activity;
            var id = UniqueId(SanitizeId(label));
            AddStep(id, label, stepType, kind == TemporalCallKind.Wait ? false : null);
            foreach (var from in predecessors)
            {
                AddTransition(from, id);
            }

            return [id];
        }

        // Invocation that might be a same-type helper: best-effort inline
        if (expression is InvocationExpressionSyntax inv &&
            TryResolveSameTypeHelper(inv, out var helperMethod))
        {
            if (helperMethod.Body != null)
            {
                return AnalyzeBlock(helperMethod.Body, predecessors);
            }

            if (helperMethod.ExpressionBody != null)
            {
                return AnalyzeExpression(helperMethod.ExpressionBody.Expression, predecessors);
            }
        }

        // Recurse into arguments of unknown invocations (e.g. wrappers)
        if (expression is InvocationExpressionSyntax unknownInv)
        {
            var current = predecessors;
            foreach (var arg in unknownInv.ArgumentList.Arguments)
            {
                current = AnalyzeExpression(arg.Expression, current);
            }

            return current;
        }

        if (expression is ConditionalExpressionSyntax conditional)
        {
            return AnalyzeConditional(conditional, predecessors);
        }

        return predecessors;
    }

    private IReadOnlyList<string> AnalyzeIf(IfStatementSyntax ifStmt, IReadOnlyList<string> predecessors)
    {
        // Condition may contain activity calls
        var afterCondition = AnalyzeExpression(ifStmt.Condition, predecessors);

        var decisionId = UniqueId("Decision");
        var label = Truncate(ifStmt.Condition.ToString().Replace("\n", " ").Trim(), 40);
        AddStep(decisionId, label, WorkflowStepType.Decision);
        foreach (var from in afterCondition)
        {
            AddTransition(from, decisionId);
        }

        var thenExits = AnalyzeStatement(ifStmt.Statement, [decisionId]);

        IReadOnlyList<string> elseExits;
        if (ifStmt.Else != null)
        {
            elseExits = AnalyzeStatement(ifStmt.Else.Statement, [decisionId]);
        }
        else
        {
            elseExits = [decisionId];
        }

        // Collect exit nodes: then path exits + else path exits
        var exits = new List<string>();
        exits.AddRange(thenExits.Where(e => e != decisionId));
        if (ifStmt.Else != null)
        {
            exits.AddRange(elseExits.Where(e => e != decisionId));
        }
        else
        {
            exits.Add(decisionId);
        }

        ConvertDecisionTransitionsToBranches(decisionId);

        return exits.Count > 0 ? exits.Distinct().ToList() : [decisionId];
    }

    private void ConvertDecisionTransitionsToBranches(string decisionId)
    {
        var fromDecision = _model.Transitions.Where(t => t.From == decisionId).ToList();
        if (fromDecision.Count == 0)
        {
            return;
        }

        // First edge → Yes, second → No, rest → Branch N
        for (var i = 0; i < fromDecision.Count; i++)
        {
            var t = fromDecision[i];
            var label = i == 0 ? "Yes" : i == 1 ? "No" : $"Branch{i}";
            _model.Branches.Add(new WorkflowBranchModel
            {
                DecisionId = decisionId,
                Label = label,
                TargetStepId = t.To,
                IsSuccessPath = i == 0,
                IsFailurePath = i == 1
            });
            _model.Transitions.Remove(t);
        }
    }

    private IReadOnlyList<string> AnalyzeSwitch(SwitchStatementSyntax switchStmt, IReadOnlyList<string> predecessors)
    {
        var afterExpr = AnalyzeExpression(switchStmt.Expression, predecessors);
        var decisionId = UniqueId("Switch");
        var label = Truncate(switchStmt.Expression.ToString().Replace("\n", " ").Trim(), 40);
        AddStep(decisionId, label, WorkflowStepType.Decision);
        foreach (var from in afterExpr)
        {
            AddTransition(from, decisionId);
        }

        var exits = new List<string>();
        foreach (var section in switchStmt.Sections)
        {
            var caseLabel = string.Join(", ", section.Labels.Select(l => Truncate(l.ToString().TrimEnd(':'), 20)));
            IReadOnlyList<string> current = [decisionId];
            foreach (var stmt in section.Statements)
            {
                if (stmt is BreakStatementSyntax)
                {
                    break;
                }

                current = AnalyzeStatement(stmt, current);
            }

            // Convert transitions from decision to first step into branches
            var fromDecision = _model.Transitions.Where(t => t.From == decisionId).ToList();
            foreach (var t in fromDecision)
            {
                // Only convert newly added ones that aren't already branches — all current from decision
                if (_model.Branches.Any(b => b.DecisionId == decisionId && b.TargetStepId == t.To))
                {
                    continue;
                }

                _model.Branches.Add(new WorkflowBranchModel
                {
                    DecisionId = decisionId,
                    Label = string.IsNullOrWhiteSpace(caseLabel) ? "case" : caseLabel,
                    TargetStepId = t.To
                });
                _model.Transitions.Remove(t);
            }

            exits.AddRange(current.Where(c => c != decisionId));
        }

        ConvertDecisionTransitionsToBranches(decisionId);
        return exits.Count > 0 ? exits.Distinct().ToList() : [decisionId];
    }

    private IReadOnlyList<string> AnalyzeConditional(ConditionalExpressionSyntax conditional, IReadOnlyList<string> predecessors)
    {
        var afterCond = AnalyzeExpression(conditional.Condition, predecessors);
        var decisionId = UniqueId("Decision");
        AddStep(decisionId, Truncate(conditional.Condition.ToString(), 40), WorkflowStepType.Decision);
        foreach (var from in afterCond)
        {
            AddTransition(from, decisionId);
        }

        var whenTrue = AnalyzeExpression(conditional.WhenTrue, [decisionId]);
        var whenFalse = AnalyzeExpression(conditional.WhenFalse, [decisionId]);
        ConvertDecisionTransitionsToBranches(decisionId);

        return whenTrue.Concat(whenFalse).Where(x => x != decisionId).Distinct().ToList();
    }

    private IReadOnlyList<string> AnalyzeParallel(
        IReadOnlyList<ExpressionSyntax> args,
        IReadOnlyList<string> predecessors,
        bool isWhenAny)
    {
        if (args.Count == 0)
        {
            return predecessors;
        }

        var forkFrom = predecessors;
        var branchEnds = new List<string>();

        foreach (var arg in args)
        {
            var ends = AnalyzeExpression(arg, forkFrom);
            branchEnds.AddRange(ends);
        }

        var joinId = UniqueId(isWhenAny ? "WhenAnyJoin" : "WhenAllJoin");
        AddStep(joinId, isWhenAny ? "WhenAny" : "WhenAll", WorkflowStepType.Activity);
        foreach (var end in branchEnds.Distinct())
        {
            AddTransition(end, joinId);
        }

        return [joinId];
    }

    private bool TryResolveSameTypeHelper(InvocationExpressionSyntax inv, out MethodDeclarationSyntax method)
    {
        method = null!;
        string? name = inv.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var n } => n.Identifier.Text,
            _ => null
        };

        if (name == null)
        {
            return false;
        }

        var match = _typeDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == name && !HasWorkflowRun(m));

        if (match == null)
        {
            return false;
        }

        method = match;
        return true;
    }

    private static bool HasWorkflowRun(MethodDeclarationSyntax m) =>
        RoslynWorkflowDiagramExtractor.HasAttribute(m.AttributeLists, "WorkflowRun");

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax p:
                    expression = p.Expression;
                    continue;
                case CastExpressionSyntax c:
                    expression = c.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }

    private static bool TryGetWhenAllOrAny(
        ExpressionSyntax expression,
        out List<ExpressionSyntax> args,
        out bool isWhenAny)
    {
        args = [];
        isWhenAny = false;

        if (expression is not InvocationExpressionSyntax inv)
        {
            return false;
        }

        var methodName = GetInvokedMethodName(inv);
        if (methodName is not ("WhenAll" or "WhenAny"))
        {
            return false;
        }

        isWhenAny = methodName == "WhenAny";
        args = inv.ArgumentList.Arguments.Select(a => a.Expression).ToList();
        return true;
    }

    private enum TemporalCallKind
    {
        Activity,
        ChildWorkflow,
        Delay,
        Wait
    }

    private static bool TryMatchTemporalCall(ExpressionSyntax expression, out TemporalCallKind kind, out string label)
    {
        kind = TemporalCallKind.Activity;
        label = "Activity";

        if (expression is not InvocationExpressionSyntax inv)
        {
            return false;
        }

        var methodName = GetInvokedMethodName(inv);
        if (methodName == null)
        {
            return false;
        }

        switch (methodName)
        {
            case "ExecuteActivityAsync":
            case "ExecuteLocalActivityAsync":
                kind = TemporalCallKind.Activity;
                label = ExtractActivityLabel(inv) ?? (methodName == "ExecuteLocalActivityAsync" ? "LocalActivity" : "Activity");
                return true;
            case "ExecuteChildWorkflowAsync":
                kind = TemporalCallKind.ChildWorkflow;
                label = ExtractChildWorkflowLabel(inv) ?? "ChildWorkflow";
                return true;
            case "DelayAsync":
            case "CreateTimer":
                kind = TemporalCallKind.Delay;
                label = "Delay";
                return true;
            case "WaitConditionAsync":
            case "WaitConditionWithTimeoutAsync":
                kind = TemporalCallKind.Wait;
                label = "Wait";
                return true;
            default:
                return false;
        }
    }

    private static string? GetInvokedMethodName(InvocationExpressionSyntax inv) =>
        inv.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax g => g.Identifier.Text,
            MemberAccessExpressionSyntax ma => ma.Name switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax g => g.Identifier.Text,
                _ => ma.Name.ToString()
            },
            MemberBindingExpressionSyntax mb => mb.Name switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax g => g.Identifier.Text,
                _ => mb.Name.ToString()
            },
            _ => null
        };

    private static string? ExtractActivityLabel(InvocationExpressionSyntax inv)
    {
        // Workflow.ExecuteActivityAsync((MyActs a) => a.DoWork(...), opts)
        // Workflow.ExecuteActivityAsync("ActivityName", args, opts)
        // Workflow.ExecuteActivityAsync<MyActs>(a => a.DoWork(...), opts)
        foreach (var arg in inv.ArgumentList.Arguments)
        {
            var expr = Unwrap(arg.Expression);
            if (expr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return lit.Token.ValueText;
            }

            if (expr is SimpleLambdaExpressionSyntax { Body: InvocationExpressionSyntax bodyInv })
            {
                return GetInvokedMethodName(bodyInv) ?? "Activity";
            }

            if (expr is ParenthesizedLambdaExpressionSyntax { Body: InvocationExpressionSyntax pbody })
            {
                return GetInvokedMethodName(pbody) ?? "Activity";
            }

            if (expr is SimpleLambdaExpressionSyntax { Body: AwaitExpressionSyntax { Expression: InvocationExpressionSyntax aInv } })
            {
                return GetInvokedMethodName(aInv) ?? "Activity";
            }

            if (expr is ParenthesizedLambdaExpressionSyntax { Body: AwaitExpressionSyntax { Expression: InvocationExpressionSyntax paInv } })
            {
                return GetInvokedMethodName(paInv) ?? "Activity";
            }

            // Block-bodied lambda: first invocation
            if (expr is SimpleLambdaExpressionSyntax { Body: BlockSyntax block } ||
                expr is ParenthesizedLambdaExpressionSyntax { Body: BlockSyntax })
            {
                var blockBody = expr is SimpleLambdaExpressionSyntax s
                    ? (BlockSyntax)s.Body
                    : (BlockSyntax)((ParenthesizedLambdaExpressionSyntax)expr).Body;
                var firstInv = blockBody.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (firstInv != null)
                {
                    return GetInvokedMethodName(firstInv) ?? "Activity";
                }
            }
        }

        // Generic type arg: ExecuteActivityAsync<Activities>
        if (inv.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax g } &&
            g.TypeArgumentList.Arguments.Count > 0)
        {
            return g.TypeArgumentList.Arguments[0].ToString().Split('.').Last();
        }

        return null;
    }

    private static string? ExtractChildWorkflowLabel(InvocationExpressionSyntax inv)
    {
        foreach (var arg in inv.ArgumentList.Arguments)
        {
            var expr = Unwrap(arg.Expression);
            if (expr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return lit.Token.ValueText;
            }

            if (expr is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
            {
                // ((ChildWf w) => w.RunAsync(...)) — type from parameter
                var paramType = expr switch
                {
                    SimpleLambdaExpressionSyntax { Parameter: var p } => p.Type?.ToString(),
                    ParenthesizedLambdaExpressionSyntax pl when pl.ParameterList.Parameters.Count > 0 =>
                        pl.ParameterList.Parameters[0].Type?.ToString(),
                    _ => null
                };
                if (!string.IsNullOrEmpty(paramType))
                {
                    return paramType.Split('.').Last();
                }

                var bodyInv = expr.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (bodyInv != null)
                {
                    return GetInvokedMethodName(bodyInv);
                }
            }
        }

        if (inv.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax g } &&
            g.TypeArgumentList.Arguments.Count > 0)
        {
            return g.TypeArgumentList.Arguments[0].ToString().Split('.').Last();
        }

        return null;
    }

    private void EnsureEndNode()
    {
        if (_model.Steps.Any(s => s.Id == "End"))
        {
            return;
        }

        _model.Steps.Add(new WorkflowStepModel
        {
            Id = "End",
            Label = "Complete",
            Order = int.MaxValue,
            StepType = WorkflowStepType.End,
            IsSuccess = true
        });
        _usedIds.Add("End");
    }

    private void AddStep(string id, string label, WorkflowStepType stepType, bool? unused = null)
    {
        _ = unused;
        if (_model.Steps.Any(s => s.Id == id))
        {
            return;
        }

        _model.Steps.Add(new WorkflowStepModel
        {
            Id = id,
            Label = label,
            Order = ++_order,
            StepType = stepType,
            IsSuccess = stepType == WorkflowStepType.End
        });
        _usedIds.Add(id);
    }

    private void AddTransition(string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to)
        {
            return;
        }

        if (_model.Transitions.Any(t => t.From == from && t.To == to))
        {
            return;
        }

        // Don't add normal transitions from decision nodes — use branches
        if (_model.Steps.Any(s => s.Id == from && s.StepType == WorkflowStepType.Decision))
        {
            if (!_model.Branches.Any(b => b.DecisionId == from && b.TargetStepId == to))
            {
                // Temporary; ConvertDecisionTransitionsToBranches or caller will set labels
                _model.Transitions.Add(new WorkflowTransitionModel { From = from, To = to });
            }

            return;
        }

        _model.Transitions.Add(new WorkflowTransitionModel { From = from, To = to });
    }

    private string UniqueId(string baseId)
    {
        var clean = string.IsNullOrWhiteSpace(baseId) ? $"Step{++_anonCounter}" : baseId;
        clean = MermaidDiagramRenderer.SanitizeNodeName(clean);
        if (string.IsNullOrEmpty(clean))
        {
            clean = $"Step{++_anonCounter}";
        }

        if (_usedIds.Add(clean))
        {
            return clean;
        }

        var i = 2;
        while (!_usedIds.Add(clean + i))
        {
            i++;
        }

        return clean + i;
    }

    private static string SanitizeId(string label) =>
        MermaidDiagramRenderer.SanitizeNodeName(label);

    private static string Truncate(string text, int max)
    {
        text = text.Trim();
        if (text.Length <= max)
        {
            return text;
        }

        return text[..(max - 1)] + "…";
    }
}
