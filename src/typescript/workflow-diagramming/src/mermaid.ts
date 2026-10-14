import type { DiagramStep } from "./types.js";

/** Render a linear flowchart TD for v1. */
export function renderMermaid(steps: DiagramStep[]): string {
  const lines: string[] = ["flowchart TD", "    Start([Start])"];
  const ids: string[] = ["Start"];

  for (const step of steps) {
    const nodeId = sanitize(step.id);
    ids.push(nodeId);
    if (step.kind === "wait") {
      lines.push(`    ${nodeId}["👤 ${escapeLabel(step.label)}"]`);
    } else {
      lines.push(`    ${nodeId}["${escapeLabel(step.label)}"]`);
    }
  }

  lines.push(`    End(["Complete"])`);
  ids.push("End");

  for (let i = 0; i < ids.length - 1; i++) {
    lines.push(`    ${ids[i]} --> ${ids[i + 1]}`);
  }

  return lines.join("\n") + "\n";
}

export function sanitize(name: string): string {
  const cleaned = name.replace(/[^a-zA-Z0-9]/g, "");
  return cleaned.length > 0 ? cleaned : "Node";
}

function escapeLabel(label: string): string {
  return label.replace(/"/g, "'");
}
