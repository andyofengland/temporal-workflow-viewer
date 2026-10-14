package com.temporaldashboard.workflowdiagramming

import java.nio.file.Files
import java.nio.file.Path
import java.time.Instant
import java.util.zip.ZipEntry
import java.util.zip.ZipOutputStream
import kotlin.io.path.createDirectories
import kotlin.io.path.writeText

data class WriteOptions(
    val sourceDir: Path,
    val outDir: Path,
    val packageName: String? = null,
    val packageVersion: String = "0.0.0",
    val createZip: Boolean = false,
)

data class WriteResult(
    val outDir: Path,
    val metadata: WorkflowDiagramsMetadata,
    val zipPath: Path? = null,
)

object Writer {
    const val GENERATOR = "workflow-diagramming-kotlin/0.1.0"

    fun write(opts: WriteOptions): WriteResult {
        val sourceDir = opts.sourceDir.toAbsolutePath().normalize()
        val outDir = opts.outDir.toAbsolutePath().normalize()
        outDir.createDirectories()

        val workflows = Extractor.extract(sourceDir)
        val packageName = opts.packageName?.trim()?.takeIf { it.isNotEmpty() }
            ?: sourceDir.fileName.toString()

        val entries = mutableListOf<WorkflowEntry>()
        val used = mutableSetOf<String>()
        for (wf in workflows) {
            var base = Extractor.sanitize(wf.name).ifEmpty { "Workflow" }
            var fileBase = base
            var n = 2
            while (!used.add(fileBase.lowercase())) {
                fileBase = "$base$n"
                n++
            }
            val diagramFile = "$fileBase.mermaid"
            outDir.resolve(diagramFile).writeText(MermaidRenderer.render(wf.steps))
            entries += WorkflowEntry(wf.name, wf.displayName, diagramFile)
        }

        val meta = WorkflowDiagramsMetadata(
            packageName = packageName,
            packageVersion = opts.packageVersion,
            packagePath = sourceDir.toString(),
            language = "Kotlin",
            targetFramework = "jvm${System.getProperty("java.specification.version")}",
            buildDateUtc = Instant.now().toString(),
            generator = GENERATOR,
            workflows = entries,
        )

        outDir.resolve("workflow-diagrams-metadata.json").writeText(toJson(meta))

        var zipPath: Path? = null
        if (opts.createZip) {
            zipPath = outDir.resolve("workflow-diagrams.zip")
            writeZip(outDir, zipPath, meta)
        }
        return WriteResult(outDir, meta, zipPath)
    }

    private fun writeZip(dir: Path, zipPath: Path, meta: WorkflowDiagramsMetadata) {
        ZipOutputStream(Files.newOutputStream(zipPath)).use { zos ->
            fun add(name: String) {
                zos.putNextEntry(ZipEntry(name))
                zos.write(Files.readAllBytes(dir.resolve(name)))
                zos.closeEntry()
            }
            add("workflow-diagrams-metadata.json")
            meta.workflows.forEach { add(it.diagramFile) }
        }
    }

    internal fun toJson(meta: WorkflowDiagramsMetadata): String {
        val sb = StringBuilder()
        sb.appendLine("{")
        sb.appendLine("  \"packageName\": ${quote(meta.packageName)},")
        sb.appendLine("  \"packageVersion\": ${quote(meta.packageVersion)},")
        sb.appendLine("  \"packagePath\": ${quote(meta.packagePath)},")
        sb.appendLine("  \"language\": ${quote(meta.language)},")
        sb.appendLine("  \"targetFramework\": ${quote(meta.targetFramework)},")
        sb.appendLine("  \"buildDateUtc\": ${quote(meta.buildDateUtc)},")
        sb.appendLine("  \"generator\": ${quote(meta.generator)},")
        sb.appendLine("  \"workflows\": [")
        meta.workflows.forEachIndexed { i, w ->
            sb.appendLine("    {")
            sb.appendLine("      \"name\": ${quote(w.name)},")
            sb.appendLine("      \"displayName\": ${quote(w.displayName)},")
            sb.appendLine("      \"diagramFile\": ${quote(w.diagramFile)}")
            sb.append("    }")
            if (i < meta.workflows.lastIndex) sb.append(",")
            sb.appendLine()
        }
        sb.appendLine("  ]")
        sb.appendLine("}")
        return sb.toString()
    }

    private fun quote(s: String?): String {
        if (s == null) return "null"
        return "\"" + s.replace("\\", "\\\\").replace("\"", "\\\"") + "\""
    }
}
