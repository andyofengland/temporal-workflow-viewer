# Go workflow diagramming

Extract Mermaid workflow diagrams from Temporal Go workflow source using `go/parser` + `go/ast`.

Module: `github.com/andyofengland/temporal-workflow-viewer/go/workflowdiagramming`

Emits the [shared package format](../../shared/README.md).

## Usage

```bash
cd src/go/workflowdiagramming
go run ./cmd/extract --dir examples/sample_workflows --out diagrams --package-name sample-go-workflows --zip
```

### Flags

| Flag | Description |
|------|-------------|
| `--dir` | Source directory to scan (required) |
| `--out` | Output directory (default: `<dir>/diagrams`) |
| `--package-name` | `packageName` in metadata |
| `--package-version` | `packageVersion` in metadata |
| `--zip` | Also write `workflow-diagrams.zip` |

## What it detects (v1)

- `workflow.ExecuteActivity`
- `workflow.ExecuteChildWorkflow`
- `workflow.Sleep`
- `workflow.Await`
- `workflow.Go` (selector / parallel marker)

Exported functions whose bodies contain those calls are treated as workflows.

## Test

```bash
go test ./...
```

## Example

See [`examples/sample_workflows/order_workflow.go`](examples/sample_workflows/order_workflow.go).
