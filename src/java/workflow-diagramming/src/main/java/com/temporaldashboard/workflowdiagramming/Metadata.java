package com.temporaldashboard.workflowdiagramming;

import java.util.ArrayList;
import java.util.List;

/** Shared zip metadata contract (packageName primary — no assemblyName). */
public final class Metadata {
    public String packageName;
    public String packageVersion;
    public String packagePath;
    public String language;
    public String targetFramework;
    public String buildDateUtc;
    public String generator;
    public List<WorkflowEntry> workflows = new ArrayList<>();

    public static final class WorkflowEntry {
        public String name;
        public String displayName;
        public String diagramFile;

        public WorkflowEntry(String name, String displayName, String diagramFile) {
            this.name = name;
            this.displayName = displayName;
            this.diagramFile = diagramFile;
        }
    }
}
