package workflowdiagramming

import (
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"path/filepath"
	"strconv"
	"strings"
)

// ExtractWorkflows walks sourceDir for .go files and finds Temporal workflow patterns.
func ExtractWorkflows(sourceDir string) ([]ExtractedWorkflow, error) {
	var results []ExtractedWorkflow
	err := filepath.Walk(sourceDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		if info.IsDir() {
			base := info.Name()
			if base == "vendor" || base == ".git" || base == "diagrams" {
				return filepath.SkipDir
			}
			return nil
		}
		if !strings.HasSuffix(path, ".go") || strings.HasSuffix(path, "_test.go") {
			return nil
		}
		wfs, err := extractFile(path)
		if err != nil {
			return err
		}
		results = append(results, wfs...)
		return nil
	})
	return results, err
}

func extractFile(path string) ([]ExtractedWorkflow, error) {
	fset := token.NewFileSet()
	f, err := parser.ParseFile(fset, path, nil, parser.ParseComments)
	if err != nil {
		return nil, err
	}

	var out []ExtractedWorkflow
	for _, decl := range f.Decls {
		fn, ok := decl.(*ast.FuncDecl)
		if !ok || fn.Body == nil || fn.Name == nil {
			continue
		}
		if !isExported(fn.Name.Name) {
			continue
		}
		if !looksLikeWorkflow(fn) {
			continue
		}
		steps := collectSteps(fn.Body)
		out = append(out, ExtractedWorkflow{
			Name:        fn.Name.Name,
			DisplayName: fn.Name.Name,
			Steps:       steps,
			SourceFile:  path,
		})
	}
	return out, nil
}

func isExported(name string) bool {
	return len(name) > 0 && name[0] >= 'A' && name[0] <= 'Z'
}

func looksLikeWorkflow(fn *ast.FuncDecl) bool {
	found := false
	ast.Inspect(fn.Body, func(n ast.Node) bool {
		if found {
			return false
		}
		call, ok := n.(*ast.CallExpr)
		if !ok {
			return true
		}
		name := callName(call)
		if isTemporalCall(name) {
			found = true
			return false
		}
		return true
	})
	return found
}

func isTemporalCall(name string) bool {
	switch {
	case strings.HasSuffix(name, "ExecuteActivity"),
		strings.HasSuffix(name, "ExecuteChildWorkflow"),
		strings.HasSuffix(name, "Sleep"),
		strings.HasSuffix(name, "Await"),
		name == "workflow.Go",
		strings.HasSuffix(name, ".Go"):
		return true
	default:
		return false
	}
}

func collectSteps(body *ast.BlockStmt) []DiagramStep {
	var steps []DiagramStep
	counter := 0
	add := func(label string, kind StepKind) {
		counter++
		steps = append(steps, DiagramStep{
			ID:    "step" + strconv.Itoa(counter) + "_" + sanitizeID(label),
			Label: label,
			Kind:  kind,
		})
	}

	ast.Inspect(body, func(n ast.Node) bool {
		call, ok := n.(*ast.CallExpr)
		if !ok {
			return true
		}
		name := callName(call)
		switch {
		case strings.HasSuffix(name, "ExecuteActivity"):
			add(activityLabel(call), StepActivity)
		case strings.HasSuffix(name, "ExecuteChildWorkflow"):
			add(activityLabel(call), StepChild)
		case strings.HasSuffix(name, "Sleep"):
			add("Sleep", StepWait)
		case strings.HasSuffix(name, "Await"):
			add("Await", StepWait)
		case name == "workflow.Go" || strings.HasSuffix(name, ".Go"):
			add("Go (selector)", StepParallel)
		}
		return true
	})
	return steps
}

func callName(call *ast.CallExpr) string {
	switch f := call.Fun.(type) {
	case *ast.SelectorExpr:
		if id, ok := f.X.(*ast.Ident); ok {
			return id.Name + "." + f.Sel.Name
		}
		return exprString(f.X) + "." + f.Sel.Name
	case *ast.Ident:
		return f.Name
	default:
		return ""
	}
}

func exprString(e ast.Expr) string {
	switch v := e.(type) {
	case *ast.Ident:
		return v.Name
	case *ast.SelectorExpr:
		return exprString(v.X) + "." + v.Sel.Name
	default:
		return ""
	}
}

func activityLabel(call *ast.CallExpr) string {
	if len(call.Args) == 0 {
		return "Activity"
	}
	// ExecuteActivity(ctx, ActivityFunc, ...) or ExecuteActivity(ctx, "Name", ...)
	arg := call.Args[0]
	if len(call.Args) >= 2 {
		arg = call.Args[1]
	}
	switch v := arg.(type) {
	case *ast.Ident:
		return v.Name
	case *ast.SelectorExpr:
		return v.Sel.Name
	case *ast.BasicLit:
		if v.Kind == token.STRING {
			s, err := strconv.Unquote(v.Value)
			if err == nil {
				return s
			}
			return strings.Trim(v.Value, `"`)
		}
	}
	return "Activity"
}

func sanitizeID(label string) string {
	var b strings.Builder
	for _, r := range label {
		if (r >= 'a' && r <= 'z') || (r >= 'A' && r <= 'Z') || (r >= '0' && r <= '9') {
			b.WriteRune(r)
		}
	}
	s := b.String()
	if s == "" {
		return "step"
	}
	if len(s) > 40 {
		return s[:40]
	}
	return s
}
