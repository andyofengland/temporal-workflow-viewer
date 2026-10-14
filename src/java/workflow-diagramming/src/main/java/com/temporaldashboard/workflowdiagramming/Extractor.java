package com.temporaldashboard.workflowdiagramming;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Stream;

/**
 * Regex-based v1 extractor for Temporal Java workflows.
 * Detects @WorkflowMethod, ActivityStub, Async.function, Workflow.sleep, Workflow.await.
 */
public final class Extractor {
    private static final Pattern WORKFLOW_METHOD = Pattern.compile(
            "@WorkflowMethod[\\s\\S]*?(?:public\\s+)?(?:[\\w.<>,\\[\\]\\s]+)\\s+(\\w+)\\s*\\(");

    private Extractor() {}

    public static List<ExtractedWorkflow> extract(Path sourceDir) throws IOException {
        List<ExtractedWorkflow> results = new ArrayList<>();
        try (Stream<Path> walk = Files.walk(sourceDir)) {
            walk.filter(p -> p.toString().endsWith(".java"))
                    .filter(p -> !p.toString().contains("/target/"))
                    .forEach(p -> {
                        try {
                            results.addAll(extractFile(p));
                        } catch (IOException e) {
                            throw new RuntimeException(e);
                        }
                    });
        }
        return results;
    }

    static List<ExtractedWorkflow> extractFile(Path file) throws IOException {
        String text = Files.readString(file, StandardCharsets.UTF_8);
        List<ExtractedWorkflow> out = new ArrayList<>();
        Matcher m = WORKFLOW_METHOD.matcher(text);
        while (m.find()) {
            String name = m.group(1);
            int start = m.start();
            int end = findMethodBodyEnd(text, m.end());
            String body = end > start ? text.substring(start, end) : text.substring(start);
            if (!looksLikeTemporal(body)) {
                continue;
            }
            ExtractedWorkflow wf = new ExtractedWorkflow(name, name, file.toString());
            wf.steps.addAll(collectSteps(body));
            out.add(wf);
        }
        return out;
    }

    private static boolean looksLikeTemporal(String body) {
        return body.contains("ActivityStub")
                || body.contains("Async.function")
                || body.contains("Workflow.sleep")
                || body.contains("Workflow.await")
                || body.contains("newActivityStub")
                || body.contains("newChildWorkflowStub")
                || Pattern.compile("\\bactivities\\.\\w+\\s*\\(").matcher(body).find();
    }

    private static List<DiagramStep> collectSteps(String body) {
        List<DiagramStep> steps = new ArrayList<>();
        int counter = 0;
        Matcher m = Pattern.compile(
                "Workflow\\.sleep\\s*(?:\\(|\\{)|" +
                "Workflow\\.await\\s*(?:\\(|\\{)|" +
                "Async\\.function\\s*\\(\\s*(?:[\\w.]+::)?([\\w.]+)|" +
                "Workflow\\.newChildWorkflowStub\\s*\\(\\s*([\\w.]+)|" +
                "(?:activities|activity)\\.(\\w+)\\s*\\(|" +
                "\\.execute\\s*\\(\\s*([\\w.\"]+)"
        ).matcher(body);

        while (m.find()) {
            counter++;
            String matched = m.group(0);
            String label;
            DiagramStep.Kind kind;
            if (matched.startsWith("Workflow.sleep")) {
                label = "Sleep";
                kind = DiagramStep.Kind.WAIT;
            } else if (matched.startsWith("Workflow.await")) {
                label = "Await";
                kind = DiagramStep.Kind.WAIT;
            } else if (matched.startsWith("Async.function")) {
                label = simpleName(m.group(1) != null ? m.group(1) : "Async");
                kind = DiagramStep.Kind.ACTIVITY;
            } else if (matched.startsWith("Workflow.newChildWorkflowStub")) {
                label = simpleName(m.group(2) != null ? m.group(2) : "Child");
                kind = DiagramStep.Kind.CHILD;
            } else if (m.group(3) != null) {
                label = m.group(3);
                kind = DiagramStep.Kind.ACTIVITY;
            } else if (m.group(4) != null) {
                label = simpleName(m.group(4).replace("\"", ""));
                kind = DiagramStep.Kind.ACTIVITY;
            } else {
                label = "Step";
                kind = DiagramStep.Kind.ACTIVITY;
            }
            if ("activities".equals(label) || "activity".equals(label)) {
                counter--;
                continue;
            }
            steps.add(new DiagramStep("step" + counter + "_" + sanitize(label), label, kind));
        }
        return steps;
    }

    private static String simpleName(String fqcn) {
        int idx = fqcn.lastIndexOf('.');
        return idx >= 0 ? fqcn.substring(idx + 1) : fqcn;
    }

    private static String sanitize(String label) {
        String s = label.replaceAll("[^a-zA-Z0-9]", "");
        if (s.isEmpty()) return "step";
        return s.length() > 40 ? s.substring(0, 40) : s;
    }

    private static int findMethodBodyEnd(String text, int from) {
        int brace = text.indexOf('{', from);
        if (brace < 0) return text.length();
        int depth = 0;
        for (int i = brace; i < text.length(); i++) {
            char c = text.charAt(i);
            if (c == '{') depth++;
            else if (c == '}') {
                depth--;
                if (depth == 0) return i + 1;
            }
        }
        return text.length();
    }
}
