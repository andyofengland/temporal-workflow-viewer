# TemporalDashboard.WorkflowDiagramming.Build

MSBuild task that generates Mermaid workflow diagrams at **build time** from your Temporal workflow assembly. Use this when you want to emit diagram files (e.g. `.mermaid`) as part of the build so you can ship the generated content without sharing the workflow DLL with the viewing site.

## Overview

- **Task:** `GenerateWorkflowDiagramsTask` loads your built assembly, discovers types marked with `[Workflow]` and diagramming attributes, and writes:
  - One **Mermaid** file per workflow (e.g. `MyWorkflow.mermaid`)
  - A **JSON metadata** file (`workflow-diagrams-metadata.json`) with assembly name/version, language/framework, build date, and workflow list
  - A **ZIP** file (`workflow-diagrams.zip`) containing all diagrams and the metadata for easy sharing or distribution
- **Target:** The included `.targets` file runs the task after `Build`, so these outputs are generated automatically when you build your workflow project.

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

After adding the package, build your project. In `bin/<Configuration>/net10.0/diagrams/` you get:
- `*.mermaid` – one file per workflow
- `workflow-diagrams-metadata.json` – assembly, framework, build date, workflow list
- `workflow-diagrams.zip` – all of the above in one archive for sharing

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

3. **Build.** When you run `dotnet build` on your workflow project, the `GenerateWorkflowDiagrams` target runs after `Build` and writes to `$(OutputPath)diagrams\`: one `.mermaid` file per workflow, `workflow-diagrams-metadata.json`, and `workflow-diagrams.zip` (all diagrams + metadata).

Default output folder: `bin\$(Configuration)\net10.0\diagrams\`.

## Task parameters and properties

**MSBuild property** (when using the built-in target):

| Property | Description |
|----------|-------------|
| `GenerateWorkflowDiagramsAssemblyPath` | Override the assembly the task inspects. Default is `$(OutputPath)$(AssemblyName).dll`. Set this when your workflows are in a different project’s output DLL (see [Troubleshooting](#troubleshooting)). |

**Task parameters** (when invoking the task yourself in a target):

| Parameter        | Description |
|------------------|-------------|
| `AssemblyPath`   | Full path to the built workflow DLL (required). |
| `OutputPath`     | Directory for generated files (required). Default in the shipped target: `$(OutputPath)diagrams`. |
| `FileExtension`  | Extension for generated diagram files (e.g. `.mermaid` or `.md`). Default: `.mermaid`. |
| `TargetFramework` | Target framework (e.g. `net10.0`). Optional; included in metadata when set. The default target passes `$(TargetFramework)`. |
| `Language`      | Language (e.g. `C#`). Optional; included in metadata. Default: `C#`. |
| `CreateZip`      | When `true` (default), creates `workflow-diagrams.zip` containing all diagrams and the metadata JSON. Set to `false` to skip the zip. |

## Metadata JSON

`workflow-diagrams-metadata.json` includes:

- **assemblyName**, **assemblyVersion**, **assemblyPath** – source assembly
- **language**, **targetFramework** – e.g. `C#`, `net10.0`
- **buildDateUtc** – ISO 8601 build timestamp
- **generator** – task name/version
- **workflows** – array of `{ name, displayName?, diagramFile }` for each workflow

Example:

```json
{
  "assemblyName": "MyWorkflows",
  "assemblyVersion": "1.0.0.0",
  "assemblyPath": "/path/to/bin/Release/net10.0/MyWorkflows.dll",
  "language": "C#",
  "targetFramework": "net10.0",
  "buildDateUtc": "2026-01-31T14:30:00.0000000Z",
  "generator": "TemporalDashboard.WorkflowDiagramming.Build/1.0.0",
  "workflows": [
    {
      "name": "OrderFulfillmentWorkflow",
      "displayName": "Order Fulfillment",
      "diagramFile": "OrderFulfillmentWorkflow.mermaid"
    },
    {
      "name": "PaymentWorkflow",
      "displayName": null,
      "diagramFile": "PaymentWorkflow.mermaid"
    }
  ]
}
```

## Troubleshooting

### No diagram output or empty `diagrams` folder

- **Add the package to the project that contains your workflow classes.** The task inspects the **built DLL of the project that references this package**. If you add the package only to your host/API project, that DLL often has no `[Workflow]` types, so you get an empty folder or a build message that no workflows were found.
- **Workflows must be marked with Temporal’s `[Workflow]` attribute** (`Temporalio.Workflows.WorkflowAttribute`). The task discovers only types that have this attribute.
- **If your workflows live in a separate project** (e.g. a “Workflows” class library), either:
  - Add `TemporalDashboard.WorkflowDiagramming.Build` to that workflow project and build it to get diagrams in that project’s `bin/.../diagrams/`, or
  - Keep the package on the host project and point the task at the workflow DLL by setting `GenerateWorkflowDiagramsAssemblyPath` in your host project:

```xml
<PropertyGroup>
  <!-- Point to the workflow project's output DLL (adjust path to match your layout) -->
  <GenerateWorkflowDiagramsAssemblyPath>$(OutputPath)..\MyWorkflows\bin\$(Configuration)\net10.0\MyWorkflows.dll</GenerateWorkflowDiagramsAssemblyPath>
</PropertyGroup>
```

- Build with normal or high verbosity (`dotnet build -v n` or `-v d`) to see messages such as “GenerateWorkflowDiagrams skipped: assembly not found” or “No workflow types found in …”.

## Requirements

- Your workflow assembly must reference `TemporalDashboard.WorkflowDiagramming` (and `Temporalio`) so that the task can resolve attribute types when loading your DLL.
- Workflow types must be annotated with the diagramming attributes; see **WORKFLOW_ATTRIBUTES_GUIDE.md** in the repo root.

## Dependencies

- **TemporalDashboard.WorkflowDiagramming** – attributes and `WorkflowDiagramGenerator`.
- **Microsoft.Build.Framework** / **Microsoft.Build.Utilities.Core** – MSBuild task API.
