# workflow-diagramming (Python)

Extract Mermaid workflow diagrams from Temporal Python workflow source using the `ast` module.

Emits the [shared package format](../../shared/README.md).

## Usage

```bash
cd src/python/workflow_diagramming
python3 -m workflow_diagramming extract examples/sample_workflows \
  --out diagrams --package-name sample-python-workflows --zip
```

Or install editable and use the console script:

```bash
pip install -e .
workflow-diagramming extract examples/sample_workflows --out diagrams --zip
```

## Options

| Flag | Description |
|------|-------------|
| `extract <source-dir>` | Root directory to scan for `.py` |
| `--out <dir>` | Output directory (default: `<source-dir>/diagrams`) |
| `--package-name <name>` | `packageName` in metadata |
| `--package-version <v>` | `packageVersion` in metadata |
| `--zip` | Also write `workflow-diagrams.zip` |

## What it detects (v1)

- `@workflow.defn` decorated functions
- `workflow.execute_activity`
- `workflow.execute_child_workflow`
- `workflow.sleep`
- `workflow.wait_condition`
- `asyncio.gather`

## Test

```bash
PYTHONPATH=. python3 -m unittest tests.test_extract
# or, if pytest is installed:
PYTHONPATH=. python3 -m pytest tests/
```

## Example

See [`examples/sample_workflows/order_workflow.py`](examples/sample_workflows/order_workflow.py).
