# TemporalDashboard.WorkflowDiagramming.Roslyn.Build

MSBuild task that generates Mermaid workflow diagrams at **build time** from your Temporal workflow **C# source** using Roslyn. No diagramming attributes and no application code required — add the package and build.

## Overview

- **Task:** `GenerateRoslynWorkflowDiagramsTask` reads `@(Compile)` sources, finds `[Workflow]` / `[WorkflowRun]` types, and writes:
  - One **Mermaid** file per workflow (e.g. `OrderWorkflow.mermaid`)
  - **JSON metadata** (`workflow-diagrams-metadata.json`)
  - **ZIP** (`workflow-diagrams.zip`) for easy sharing
- **Target:** Runs automatically after `Build` when this package is referenced.

This is the build-time counterpart to [`TemporalDashboard.WorkflowDiagramming.Roslyn`](../TemporalDashboard.WorkflowDiagramming.Roslyn/). For attribute-based build-time generation, see [`TemporalDashboard.WorkflowDiagramming.Build`](../TemporalDashboard.WorkflowDiagramming.Build/) instead (typically use one or the other).

## Usage

### Option A: NuGet package (recommended)

```bash
dotnet add package TemporalDashboard.WorkflowDiagramming.Roslyn.Build
```

Or use the install script from this repo:

```bash
./scripts/install-workflow-diagramming-roslyn-build.sh
./scripts/install-workflow-diagramming-roslyn-build.sh path/to/YourWorkflows.csproj 1.0.0
```

```powershell
.\scripts\install-workflow-diagramming-roslyn-build.ps1
.\scripts\install-workflow-diagramming-roslyn-build.ps1 -Project .\src\MyWorkflows\MyWorkflows.csproj -Version 1.0.0
```

Then `dotnet build`. Output lands in `bin/<Configuration>/net10.0/diagrams/`.

### Option B: Project reference (same repo)

```xml
<ItemGroup>
  <ProjectReference Include="path\to\TemporalDashboard.WorkflowDiagramming.Roslyn.Build\TemporalDashboard.WorkflowDiagramming.Roslyn.Build.csproj"
                    ReferenceOutputAssembly="false" />
</ItemGroup>

<Import Project="path\to\TemporalDashboard.WorkflowDiagramming.Roslyn.Build\TemporalDashboard.WorkflowDiagramming.Roslyn.Build.targets" />
```

## MSBuild properties

| Property | Description |
|----------|-------------|
| `GenerateRoslynWorkflowDiagramsOutputPath` | Override output directory. Default: `$(OutputPath)diagrams`. |

## Task parameters

| Parameter | Description |
|-----------|-------------|
| `SourceFiles` | C# sources to analyze (required). Default target passes `@(Compile)`. |
| `ProjectDirectory` | Base path for relative Compile items. |
| `OutputPath` | Directory for generated files (required). |
| `FileExtension` | Diagram extension (default `.mermaid`). |
| `TargetFramework` / `Language` / `AssemblyName` / `AssemblyVersion` | Metadata fields. |
| `CreateZip` | When `true` (default), create `workflow-diagrams.zip`. |

## Requirements

- Workflow types marked with Temporal `[Workflow]` and a `[WorkflowRun]` method body.
- Normal Temporal SDK usage (`ExecuteActivityAsync`, `if`, `Task.WhenAll`, etc.) — see the [Roslyn README](../TemporalDashboard.WorkflowDiagramming.Roslyn/README.md) for V1 patterns.
- **No** `TemporalDashboard.WorkflowDiagramming` attributes required.

## Troubleshooting

- Add this package to the **project that contains the workflow source files** (not only a host/API project that references them).
- Build with `-v n` to see “Generated Roslyn diagram” / “No workflow types found” messages.
- If you also use `TemporalDashboard.WorkflowDiagramming.Build`, both write to `diagrams/` by default — set `GenerateRoslynWorkflowDiagramsOutputPath` (or the attribute package’s assembly path) to avoid overwriting.

## Dependencies (task runtime)

Shipped inside the package `lib/` folder (development dependency — not added as compile refs to your app):

- `TemporalDashboard.WorkflowDiagramming.Roslyn`
- `TemporalDashboard.WorkflowDiagramming`
- `Microsoft.CodeAnalysis.CSharp` (and related)
- `Microsoft.Build.Framework` / `Microsoft.Build.Utilities.Core`
