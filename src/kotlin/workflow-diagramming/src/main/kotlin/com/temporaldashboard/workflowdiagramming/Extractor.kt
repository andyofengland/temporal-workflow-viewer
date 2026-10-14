package com.temporaldashboard.workflowdiagramming

import java.nio.file.Files
import java.nio.file.Path
import kotlin.io.path.name
import kotlin.io.path.readText
import kotlin.streams.asSequence

/**
 * Regex-based v1 extractor for Temporal Kotlin workflows.
 * Detects @WorkflowMethod, activity stubs, Workflow.sleep, Workflow.await.
 */
object Extractor {
    private val workflowMethod = Regex(
        """@WorkflowMethod[\s\S]*?(?:override\s+)?(?:fun|suspend\s+fun)\s+(\w+)\s*\("""
    )
    private val stepPattern = Regex(
        """Workflow\.sleep\s*(?:\(|\{)|""" +
            """Workflow\.await\s*(?:\(|\{)|""" +
            """Async\.function\s*\(\s*(?:[\w.]+::)?([\w.]+)|""" +
            """Workflow\.newChildWorkflowStub\s*\(\s*([\w.]+)|""" +
            """(?:activities|activity)\.(\w+)\s*\(|""" +
            """\.execute\s*\(\s*([\w."]+)"""
    )

    fun extract(sourceDir: Path): List<ExtractedWorkflow> {
        val results = mutableListOf<ExtractedWorkflow>()
        Files.walk(sourceDir).use { stream ->
            stream.asSequence()
                .filter { it.name.endsWith(".kt") }
                .filter { !it.toString().contains("/build/") }
                .forEach { path ->
                    results += extractFile(path)
                }
        }
        return results
    }

    internal fun extractFile(file: Path): List<ExtractedWorkflow> {
        val text = file.readText()
        val out = mutableListOf<ExtractedWorkflow>()
        for (match in workflowMethod.findAll(text)) {
            val name = match.groupValues[1]
            val bodyStart = match.range.first
            val bodyEnd = findMethodBodyEnd(text, match.range.last + 1)
            val body = text.substring(bodyStart, bodyEnd.coerceAtMost(text.length))
            if (!looksLikeTemporal(body)) continue
            out += ExtractedWorkflow(
                name = name,
                displayName = name,
                steps = collectSteps(body),
                sourceFile = file.toString(),
            )
        }
        return out
    }

    private fun looksLikeTemporal(body: String): Boolean =
        body.contains("ActivityStub") ||
            body.contains("Async.function") ||
            body.contains("Workflow.sleep") ||
            body.contains("Workflow.await") ||
            body.contains("newActivityStub") ||
            body.contains("newChildWorkflowStub") ||
            Regex("""\bactivities\.\w+\s*\(""").containsMatchIn(body)

    private fun collectSteps(body: String): List<DiagramStep> {
        val steps = mutableListOf<DiagramStep>()
        var counter = 0
        for (m in stepPattern.findAll(body)) {
            counter++
            val matched = m.value
            val (label, kind) = when {
                matched.startsWith("Workflow.sleep") -> "Sleep" to StepKind.WAIT
                matched.startsWith("Workflow.await") -> "Await" to StepKind.WAIT
                matched.startsWith("Async.function") ->
                    simpleName(m.groupValues.getOrNull(1)?.ifBlank { null } ?: "Async") to StepKind.ACTIVITY
                matched.startsWith("Workflow.newChildWorkflowStub") ->
                    simpleName(m.groupValues.getOrNull(2)?.ifBlank { null } ?: "Child") to StepKind.CHILD
                m.groupValues.getOrNull(3)?.isNotBlank() == true ->
                    m.groupValues[3] to StepKind.ACTIVITY
                m.groupValues.getOrNull(4)?.isNotBlank() == true ->
                    simpleName(m.groupValues[4].replace("\"", "")) to StepKind.ACTIVITY
                else -> "Step" to StepKind.ACTIVITY
            }
            // Skip method-reference receiver false positives (e.g. Async.function(activities::shipOrder))
            if (label == "activities" || label == "activity") continue
            steps += DiagramStep("step${counter}_${sanitize(label)}", label, kind)
        }
        return steps
    }

    private fun simpleName(fqcn: String): String =
        fqcn.substringAfterLast('.')

    fun sanitize(label: String): String {
        val s = label.replace(Regex("[^a-zA-Z0-9]"), "")
        return when {
            s.isEmpty() -> "step"
            s.length > 40 -> s.take(40)
            else -> s
        }
    }

    private fun findMethodBodyEnd(text: String, from: Int): Int {
        val brace = text.indexOf('{', from)
        if (brace < 0) return text.length
        var depth = 0
        for (i in brace until text.length) {
            when (text[i]) {
                '{' -> depth++
                '}' -> {
                    depth--
                    if (depth == 0) return i + 1
                }
            }
        }
        return text.length
    }
}
