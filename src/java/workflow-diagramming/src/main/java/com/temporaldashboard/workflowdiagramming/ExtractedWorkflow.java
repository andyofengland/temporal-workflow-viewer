package com.temporaldashboard.workflowdiagramming;

import java.util.ArrayList;
import java.util.List;

public final class ExtractedWorkflow {
    public final String name;
    public final String displayName;
    public final List<DiagramStep> steps = new ArrayList<>();
    public final String sourceFile;

    public ExtractedWorkflow(String name, String displayName, String sourceFile) {
        this.name = name;
        this.displayName = displayName;
        this.sourceFile = sourceFile;
    }
}
