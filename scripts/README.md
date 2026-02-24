# Scripts

## install-workflow-diagramming-build

Adds the Temporal Dashboard workflow diagramming NuGet packages to your project and wires up the build task so Mermaid diagrams are generated at build time.

**When you add the packages:**

- **TemporalDashboard.WorkflowDiagramming** – gives you the attributes (`[WorkflowDiagram]`, `[WorkflowStep]`, etc.) and the diagram generator for your workflow code.
- **TemporalDashboard.WorkflowDiagramming.Build** – brings in an MSBuild target that runs after `Build` and writes to `$(OutputPath)diagrams/`: one `.mermaid` file per workflow, a `workflow-diagrams-metadata.json` (assembly, framework, build date, workflow list), and a `workflow-diagrams.zip` containing all of them for easy sharing. NuGet imports the target automatically; no manual `.targets` or `Import` is required.

**Usage:**

From a directory that contains a single `.csproj` (your workflow project):

```bash
# Bash (Linux/macOS/WSL)
./scripts/install-workflow-diagramming-build.sh

# With explicit project and version
./scripts/install-workflow-diagramming-build.sh path/to/MyWorkflows.csproj 1.0.0
```

```powershell
# PowerShell (Windows/macOS/Linux)
.\scripts\install-workflow-diagramming-build.ps1

# With parameters
.\scripts\install-workflow-diagramming-build.ps1 -Project .\src\MyWorkflows\MyWorkflows.csproj -Version 1.0.0
```

**After running:**

1. Build your project: `dotnet build`
2. In `bin/<Configuration>/net10.0/diagrams/` you get: `<WorkflowName>.mermaid` files, `workflow-diagrams-metadata.json`, and `workflow-diagrams.zip` (all diagrams + metadata for sharing).
3. Use the generated Mermaid in docs, CI, or a viewer without shipping your workflow DLL; share the zip for distribution.

**Requirements:**

- .NET 10.0 SDK
- Workflow types annotated with the diagramming attributes (see [WORKFLOW_ATTRIBUTES_GUIDE.md](../WORKFLOW_ATTRIBUTES_GUIDE.md) in the repo root)

**Package source:**

Scripts use `https://api.nuget.org/v3/index.json` by default. Override with `-Source` (PowerShell) or `NUGET_SOURCE` (bash).
