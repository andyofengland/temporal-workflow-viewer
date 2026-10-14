# workflow-diagramming-kotlin

Extract Mermaid workflow diagrams from Temporal Kotlin workflow source (regex-based v1).

Emits the [shared package format](../../shared/README.md).

## Usage

```bash
cd src/kotlin/workflow-diagramming
./gradlew run --args="examples/sample-workflows --out diagrams --package-name sample-kotlin-workflows --zip"
```

If the Gradle wrapper is not present yet:

```bash
gradle wrapper --gradle-version 8.10.2
./gradlew run --args="examples/sample-workflows --out diagrams --package-name sample-kotlin-workflows --zip"
```

## Options

| Flag | Description |
|------|-------------|
| `<source-dir>` | Root directory to scan for `.kt` |
| `--out <dir>` | Output directory (default: `<source-dir>/diagrams`) |
| `--package-name <name>` | `packageName` in metadata |
| `--package-version <v>` | `packageVersion` in metadata |
| `--zip` | Also write `workflow-diagrams.zip` |

## What it detects (v1)

- `@WorkflowMethod` annotated functions
- `activities.*` / activity stub calls
- `Async.function`
- `Workflow.sleep` / `Workflow.await`
- `Workflow.newChildWorkflowStub`

## Test

```bash
./gradlew test
```

## Example

See [`examples/sample-workflows/OrderWorkflow.kt`](examples/sample-workflows/OrderWorkflow.kt).
