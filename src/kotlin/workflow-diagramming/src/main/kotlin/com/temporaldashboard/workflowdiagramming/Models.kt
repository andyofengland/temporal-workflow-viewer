package com.temporaldashboard.workflowdiagramming

/** Shared zip metadata contract (packageName primary — no assemblyName). */
data class WorkflowDiagramsMetadata(
    val packageName: String,
    val packageVersion: String = "0.0.0",
    val packagePath: String? = null,
    val language: String = "Kotlin",
    val targetFramework: String? = null,
    val buildDateUtc: String,
    val generator: String,
    val workflows: List<WorkflowEntry> = emptyList(),
)

data class WorkflowEntry(
    val name: String,
    val displayName: String? = null,
    val diagramFile: String,
)

enum class StepKind { ACTIVITY, WAIT, CHILD, PARALLEL }

data class DiagramStep(
    val id: String,
    val label: String,
    val kind: StepKind,
)

data class ExtractedWorkflow(
    val name: String,
    val displayName: String,
    val steps: List<DiagramStep>,
    val sourceFile: String,
)
