package com.temporaldashboard.workflowdiagramming

import java.nio.file.Files
import java.nio.file.Path
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class ExtractorTest {
    private val fixtures: Path = Path.of("examples/sample-workflows")

    @Test
    fun extractsOrderWorkflow() {
        assertTrue(Files.isDirectory(fixtures), "fixtures missing: ${fixtures.toAbsolutePath()}")
        val workflows = Extractor.extract(fixtures)
        assertTrue(workflows.isNotEmpty())
        val order = workflows.first { it.name == "fulfillOrder" }
        val labels = order.steps.map { it.label }
        assertTrue(labels.contains("Sleep"), "steps=$labels")
        assertTrue(labels.contains("Await"), "steps=$labels")
        assertTrue(labels.any { it == "chargePayment" || it == "Sleep" }, "steps=$labels")
    }

    @Test
    fun writesMetadataWithPackageName() {
        val tmp = Files.createTempDirectory("wf-diagrams-kt-")
        try {
            val result = Writer.write(
                WriteOptions(
                    sourceDir = fixtures,
                    outDir = tmp,
                    packageName = "sample-kotlin-workflows",
                    packageVersion = "0.1.0",
                    createZip = true,
                )
            )
            assertEquals("sample-kotlin-workflows", result.metadata.packageName)
            assertEquals("Kotlin", result.metadata.language)
            val json = Files.readString(tmp.resolve("workflow-diagrams-metadata.json"))
            assertFalse(json.contains("\"assemblyName\""))
            assertTrue(json.contains("\"packageName\""))
            assertTrue(Files.exists(result.zipPath!!))
            for (w in result.metadata.workflows) {
                val mermaid = Files.readString(tmp.resolve(w.diagramFile))
                assertTrue(mermaid.startsWith("flowchart TD"))
                assertTrue(mermaid.contains("Start([Start])"))
                assertTrue(mermaid.contains("End([\"Complete\"])"))
            }
        } finally {
            Files.walk(tmp).sorted(Comparator.reverseOrder()).forEach { Files.deleteIfExists(it) }
        }
    }
}
