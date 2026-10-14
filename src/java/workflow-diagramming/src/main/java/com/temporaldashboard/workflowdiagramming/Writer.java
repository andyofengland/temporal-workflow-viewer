package com.temporaldashboard.workflowdiagramming;

import java.io.IOException;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;

public final class Writer {
    public static final String GENERATOR = "workflow-diagramming-java/0.1.0";

    public static final class Options {
        public Path sourceDir;
        public Path outDir;
        public String packageName;
        public String packageVersion = "0.0.0";
        public boolean createZip;
    }

    public static final class Result {
        public Path outDir;
        public Metadata metadata;
        public Path zipPath;
    }

    private Writer() {}

    public static Result write(Options opts) throws IOException {
        Path sourceDir = opts.sourceDir.toAbsolutePath().normalize();
        Path outDir = opts.outDir.toAbsolutePath().normalize();
        Files.createDirectories(outDir);

        List<ExtractedWorkflow> workflows = Extractor.extract(sourceDir);
        String packageName = opts.packageName != null && !opts.packageName.isBlank()
                ? opts.packageName.trim()
                : sourceDir.getFileName().toString();

        Metadata meta = new Metadata();
        meta.packageName = packageName;
        meta.packageVersion = opts.packageVersion;
        meta.packagePath = sourceDir.toString();
        meta.language = "Java";
        meta.targetFramework = "jdk" + System.getProperty("java.specification.version");
        meta.buildDateUtc = Instant.now().toString();
        meta.generator = GENERATOR;

        Set<String> used = new HashSet<>();
        for (ExtractedWorkflow wf : workflows) {
            String base = MermaidRenderer.sanitize(wf.name);
            if (base.isEmpty()) base = "Workflow";
            String fileBase = base;
            int n = 2;
            while (!used.add(fileBase.toLowerCase())) {
                fileBase = base + n;
                n++;
            }
            String diagramFile = fileBase + ".mermaid";
            Files.writeString(outDir.resolve(diagramFile), MermaidRenderer.render(wf.steps), StandardCharsets.UTF_8);
            meta.workflows.add(new Metadata.WorkflowEntry(wf.name, wf.displayName, diagramFile));
        }

        Files.writeString(outDir.resolve("workflow-diagrams-metadata.json"), toJson(meta), StandardCharsets.UTF_8);

        Result result = new Result();
        result.outDir = outDir;
        result.metadata = meta;
        if (opts.createZip) {
            Path zipPath = outDir.resolve("workflow-diagrams.zip");
            writeZip(outDir, zipPath, meta);
            result.zipPath = zipPath;
        }
        return result;
    }

    private static void writeZip(Path dir, Path zipPath, Metadata meta) throws IOException {
        try (OutputStream fos = Files.newOutputStream(zipPath);
             ZipOutputStream zos = new ZipOutputStream(fos)) {
            addZip(zos, dir, "workflow-diagrams-metadata.json");
            for (Metadata.WorkflowEntry w : meta.workflows) {
                addZip(zos, dir, w.diagramFile);
            }
        }
    }

    private static void addZip(ZipOutputStream zos, Path dir, String name) throws IOException {
        zos.putNextEntry(new ZipEntry(name));
        zos.write(Files.readAllBytes(dir.resolve(name)));
        zos.closeEntry();
    }

    /** Minimal JSON writer — avoids extra dependencies. */
    static String toJson(Metadata meta) {
        StringBuilder sb = new StringBuilder();
        sb.append("{\n");
        sb.append("  \"packageName\": ").append(quote(meta.packageName)).append(",\n");
        sb.append("  \"packageVersion\": ").append(quote(meta.packageVersion)).append(",\n");
        sb.append("  \"packagePath\": ").append(quote(meta.packagePath)).append(",\n");
        sb.append("  \"language\": ").append(quote(meta.language)).append(",\n");
        sb.append("  \"targetFramework\": ").append(quote(meta.targetFramework)).append(",\n");
        sb.append("  \"buildDateUtc\": ").append(quote(meta.buildDateUtc)).append(",\n");
        sb.append("  \"generator\": ").append(quote(meta.generator)).append(",\n");
        sb.append("  \"workflows\": [\n");
        for (int i = 0; i < meta.workflows.size(); i++) {
            Metadata.WorkflowEntry w = meta.workflows.get(i);
            sb.append("    {\n");
            sb.append("      \"name\": ").append(quote(w.name)).append(",\n");
            sb.append("      \"displayName\": ").append(quote(w.displayName)).append(",\n");
            sb.append("      \"diagramFile\": ").append(quote(w.diagramFile)).append("\n");
            sb.append("    }");
            if (i < meta.workflows.size() - 1) sb.append(",");
            sb.append("\n");
        }
        sb.append("  ]\n");
        sb.append("}\n");
        return sb.toString();
    }

    private static String quote(String s) {
        if (s == null) return "null";
        return "\"" + s.replace("\\", "\\\\").replace("\"", "\\\"") + "\"";
    }
}
