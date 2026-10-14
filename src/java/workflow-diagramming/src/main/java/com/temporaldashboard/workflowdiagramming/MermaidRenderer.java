package com.temporaldashboard.workflowdiagramming;

import java.util.ArrayList;
import java.util.List;

public final class MermaidRenderer {
    private MermaidRenderer() {}

    public static String render(List<DiagramStep> steps) {
        StringBuilder sb = new StringBuilder();
        sb.append("flowchart TD\n");
        sb.append("    Start([Start])\n");
        List<String> ids = new ArrayList<>();
        ids.add("Start");
        for (DiagramStep step : steps) {
            String id = sanitize(step.id);
            ids.add(id);
            String label = step.label.replace("\"", "'");
            if (step.kind == DiagramStep.Kind.WAIT) {
                sb.append("    ").append(id).append("[\"👤 ").append(label).append("\"]\n");
            } else {
                sb.append("    ").append(id).append("[\"").append(label).append("\"]\n");
            }
        }
        sb.append("    End([\"Complete\"])\n");
        ids.add("End");
        for (int i = 0; i < ids.size() - 1; i++) {
            sb.append("    ").append(ids.get(i)).append(" --> ").append(ids.get(i + 1)).append("\n");
        }
        return sb.toString();
    }

    public static String sanitize(String name) {
        String s = name.replaceAll("[^a-zA-Z0-9]", "");
        return s.isEmpty() ? "Node" : s;
    }
}
