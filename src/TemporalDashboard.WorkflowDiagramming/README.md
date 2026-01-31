# TemporalDashboard.WorkflowDiagramming

Workflow diagram generation library for Temporal workflows using Mermaid syntax.

## Overview

This assembly provides:
- Workflow diagram attributes for annotating workflow code
- Mermaid diagram generator that creates visual representations of workflows
- Support for parallel flows, decisions, branches, and AI-powered activities

## Components

### Attributes

All workflow diagram attributes are in the `TemporalDashboard.WorkflowDiagramming.Attributes` namespace:

- `[WorkflowDiagram]` - Workflow-level metadata
- `[WorkflowStart]` - Marks workflow start
- `[WorkflowStep]` - Marks workflow steps/activities
- `[WorkflowDecision]` - Marks decision points
- `[WorkflowBranch]` - Defines branches from decisions
- `[WorkflowHumanApproval]` - Marks human-in-the-loop steps
- `[WorkflowEnd]` - Marks workflow completion
- `[WorkflowTransition]` - Defines transitions between steps

### WorkflowDiagramGenerator

Static class that generates Mermaid flowchart syntax from workflow classes:

```csharp
var mermaidDiagram = WorkflowDiagramGenerator.GenerateMermaidDiagram(typeof(MyWorkflow));
```

## Features

- **Parallel Flow Detection**: Automatically detects and visualizes parallel execution
- **AI Activity Highlighting**: Special styling for AI-powered activities (🤖 emoji + purple color)
- **Decision Points**: Visual representation of decision nodes and branches
- **Human Approval Steps**: Special styling for human-in-the-loop steps

## Usage

1. Annotate your workflow class and methods with the attributes
2. Call `WorkflowDiagramGenerator.GenerateMermaidDiagram()` with your workflow type
3. Render the Mermaid syntax in your UI (e.g., using Mermaid.js)

## Dependencies

- `Temporalio` - For `WorkflowRunAttribute` detection
