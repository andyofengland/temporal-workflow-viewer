package com.temporaldashboard.workflowdiagramming;

import org.junit.jupiter.api.Test;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.stream.Collectors;

import static org.junit.jupiter.api.Assertions.*;

class ExtractorTest {

    @Test
    void extractsOrderWorkflow() throws Exception {
        Path fixtures = Path.of("examples/sample-workflows");
        assertTrue(Files.isDirectory(fixtures), "fixtures missing: " + fixtures.toAbsolutePath());
        List<ExtractedWorkflow> workflows = Extractor.extract(fixtures);
        assertFalse(workflows.isEmpty());
        ExtractedWorkflow order = workflows.stream()
                .filter(w -> "fulfillOrder".equals(w.name))
                .findFirst()
                .orElseThrow();
        List<String> labels = order.steps.stream().map(s -> s.label).collect(Collectors.toList());
        assertTrue(labels.contains("chargePayment") || labels.contains("Sleep"),
                "unexpected steps: " + labels);
        assertTrue(labels.contains("Sleep"));
        assertTrue(labels.contains("Await"));
    }

    @Test
    void writesMetadataWithPackageName() throws Exception {
        Path tmp = Files.createTempDirectory("wf-diagrams-java-");
        try {
            Writer.Options opts = new Writer.Options();
            opts.sourceDir = Path.of("examples/sample-workflows");
            opts.outDir = tmp;
            opts.packageName = "sample-java-workflows";
            opts.packageVersion = "0.1.0";
            opts.createZip = true;

            Writer.Result result = Writer.write(opts);
            assertEquals("sample-java-workflows", result.metadata.packageName);
            assertEquals("Java", result.metadata.language);
            String json = Files.readString(tmp.resolve("workflow-diagrams-metadata.json"));
            assertFalse(json.contains("\"assemblyName\""));
            assertTrue(json.contains("\"packageName\""));
            assertTrue(Files.exists(result.zipPath));
            for (Metadata.WorkflowEntry w : result.metadata.workflows) {
                String mermaid = Files.readString(tmp.resolve(w.diagramFile));
                assertTrue(mermaid.startsWith("flowchart TD"));
                assertTrue(mermaid.contains("Start([Start])"));
                assertTrue(mermaid.contains("End([\"Complete\"])"));
            }
        } finally {
            // best-effort cleanup
            try (var walk = Files.walk(tmp)) {
                walk.sorted((a, b) -> b.compareTo(a)).forEach(p -> {
                    try { Files.deleteIfExists(p); } catch (Exception ignored) {}
                });
            }
        }
    }
}
