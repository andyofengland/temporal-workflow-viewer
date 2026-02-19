# TemporalDashboard.WorkflowDiagramming.Build

MSBuild task that generates Mermaid workflow diagrams at **build time** from your Temporal workflow assembly. Use this when you want to emit diagram files (e.g. `.mermaid`) as part of the build so you can ship the generated content without sharing the workflow DLL with the viewing site.

## Overview

- **Task:** `GenerateWorkflowDiagramsTask` loads your built assembly, discovers types marked with `[Workflow]` and diagramming attributes, and writes one Mermaid file per workflow to an output directory.
- **Target:** The included `.targets` file runs the task after `Build`, so diagrams are generated automatically when you build your workflow project.

## Usage

### Option A: NuGet package (recommended)

When the package is published to NuGet, add it to your workflow project. The build task is wired **automatically** (no manual `Import` needed):

```bash
dotnet add package TemporalDashboard.WorkflowDiagramming
dotnet add package TemporalDashboard.WorkflowDiagramming.Build
```

Or use the install script from this repo (adds both packages and works with a local or NuGet source):

```bash
# From your workflow project directory
./scripts/install-workflow-diagramming-build.sh

# Or with explicit project and version
./scripts/install-workflow-diagramming-build.sh path/to/YourWorkflows.csproj 1.0.0
```

```powershell
.\scripts\install-workflow-diagramming-build.ps1
.\scripts\install-workflow-diagramming-build.ps1 -Project .\src\MyWorkflows\MyWorkflows.csproj -Version 1.0.0
```

After adding the package, build your project; `.mermaid` files appear in `bin/<Configuration>/net10.0/diagrams/`.

### Option B: Project reference (e.g. same repo)

1. **Reference the Build project.** In your **workflow project** (the one that contains your Temporal workflows and diagramming attributes), add a project reference to this Build project:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\TemporalDashboard.WorkflowDiagramming.Build\TemporalDashboard.WorkflowDiagramming.Build.csproj"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

`ReferenceOutputAssembly="false"` keeps the task assembly from being copied into your app’s output; it is only used by MSBuild.

2. **Import the targets.** In the same workflow project file, import the targets so the diagram generation runs after build:

```xml
<Import Project="path\to\TemporalDashboard.WorkflowDiagramming.Build\TemporalDashboard.WorkflowDiagramming.Build.targets" />
```

Example (if your workflow project is in the same repo, e.g. under `src/MyWorkflows/`):

```xml
<Import Project="..\TemporalDashboard.WorkflowDiagramming.Build\TemporalDashboard.WorkflowDiagramming.Build.targets" />
```

3. **Build.** When you run `dotnet build` on your workflow project:

1. The project builds and produces its DLL.
2. The `GenerateWorkflowDiagrams` target runs after `Build`.
3. The task loads that DLL, finds all `[Workflow]` types with diagramming attributes, and writes one `.mermaid` file per workflow to `$(OutputPath)diagrams\`.

Default output: `bin\$(Configuration)\net10.0\diagrams\*.mermaid`.

## Task parameters

You can override the default behaviour by calling the task yourself in a target and setting:

| Parameter       | Description |
|----------------|-------------|
| `AssemblyPath` | Full path to the built workflow DLL (required). |
| `OutputPath`   | Directory for generated files (required). Default in the shipped target: `$(OutputPath)diagrams`. |
| `FileExtension` | Extension for generated files (e.g. `.mermaid` or `.md`). Default: `.mermaid`. |

## Requirements

- Your workflow assembly must reference `TemporalDashboard.WorkflowDiagramming` (and `Temporalio`) so that the task can resolve attribute types when loading your DLL.
- Workflow types must be annotated with the diagramming attributes; see **WORKFLOW_ATTRIBUTES_GUIDE.md** in the repo root.

## Dependencies

- **TemporalDashboard.WorkflowDiagramming** – attributes and `WorkflowDiagramGenerator`.
- **Microsoft.Build.Framework** / **Microsoft.Build.Utilities.Core** – MSBuild task API.
