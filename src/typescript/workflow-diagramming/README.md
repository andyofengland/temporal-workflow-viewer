# @temporal-dashboard/workflow-diagramming-typescript

Extract Mermaid workflow diagrams from Temporal TypeScript workflow source.

Emits the [shared package format](../../shared/README.md): `workflow-diagrams-metadata.json` + one `*.mermaid` file per workflow (optional zip).

## Install / run

```bash
cd src/typescript/workflow-diagramming
npm install
npx tsx src/cli.ts examples/sample-workflows --out diagrams --package-name sample-ts-workflows --zip
```

Or after `npm run build`:

```bash
node dist/cli.js <source-dir> --out diagrams --package-name my-workflows --zip
```

Bin name: `workflow-diagramming-ts`.

## Options

| Flag | Description |
|------|-------------|
| `<source-dir>` | Root directory to scan for `.ts` / `.tsx` / `.js` |
| `--out <dir>` | Output directory (default: `<source-dir>/diagrams`) |
| `--package-name <name>` | `packageName` in metadata |
| `--package-version <v>` | `packageVersion` in metadata |
| `--zip` | Also write `workflow-diagrams.zip` |

## What it detects (v1)

- `proxyActivities` bindings (destructured or namespaced)
- Exported async functions that call those activities
- `workflow.executeChild` / `executeChild`
- `condition(` / `workflow.condition`
- `sleep(` / `workflow.sleep`
- `Promise.all(` / `workflow.promise.all`

Mermaid output is a linear `flowchart TD` with Start, activity nodes, wait nodes as `["👤 …"]`, and End.

## Test

```bash
npm test
```

## Example fixture

See [`examples/sample-workflows/orderWorkflow.ts`](examples/sample-workflows/orderWorkflow.ts).
