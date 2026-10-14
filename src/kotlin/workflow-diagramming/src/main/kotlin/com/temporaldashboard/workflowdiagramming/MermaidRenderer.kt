package com.temporaldashboard.workflowdiagramming

object MermaidRenderer {
    fun render(steps: List<DiagramStep>): String {
        val sb = StringBuilder()
        sb.appendLine("flowchart TD")
        sb.appendLine("    Start([Start])")
        val ids = mutableListOf("Start")
        for (step in steps) {
            val id = Extractor.sanitize(step.id).ifEmpty { "Node" }
            ids += id
            val label = step.label.replace("\"", "'")
            if (step.kind == StepKind.WAIT) {
                sb.appendLine("    $id[\"👤 $label\"]")
            } else {
                sb.appendLine("    $id[\"$label\"]")
            }
        }
        sb.appendLine("    End([\"Complete\"])")
        ids += "End"
        for (i in 0 until ids.size - 1) {
            sb.appendLine("    ${ids[i]} --> ${ids[i + 1]}")
        }
        return sb.toString()
    }
}
