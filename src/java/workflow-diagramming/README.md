# workflow-diagramming-java

Extract Mermaid workflow diagrams from Temporal Java workflow source (regex-based v1).

Emits the [shared package format](../../shared/README.md).

## Usage

```bash
cd src/java/workflow-diagramming
mvn -q package
mvn -q exec:java -Dexec.args="examples/sample-workflows --out diagrams --package-name sample-java-workflows --zip"
```

Or run the shaded/main jar:

```bash
java -jar target/workflow-diagramming-java-0.1.0.jar examples/sample-workflows --out diagrams --package-name sample-java-workflows --zip
```

## Options

| Flag | Description |
|------|-------------|
| `<source-dir>` | Root directory to scan for `.java` |
| `--out <dir>` | Output directory (default: `<source-dir>/diagrams`) |
| `--package-name <name>` | `packageName` in metadata |
| `--package-version <v>` | `packageVersion` in metadata |
| `--zip` | Also write `workflow-diagrams.zip` |

## What it detects (v1)

- `@WorkflowMethod` annotated methods
- `activities.*` / activity stub calls
- `Async.function`
- `Workflow.sleep` / `Workflow.await`
- `Workflow.newChildWorkflowStub`

## Test

```bash
mvn test
```

## Example

See [`examples/sample-workflows/OrderWorkflow.java`](examples/sample-workflows/OrderWorkflow.java).
