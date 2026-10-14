package com.temporaldashboard.workflowdiagramming;

public final class DiagramStep {
    public enum Kind { ACTIVITY, WAIT, CHILD, PARALLEL }

    public final String id;
    public final String label;
    public final Kind kind;

    public DiagramStep(String id, String label, Kind kind) {
        this.id = id;
        this.label = label;
        this.kind = kind;
    }
}
