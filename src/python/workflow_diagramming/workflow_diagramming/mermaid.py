from __future__ import annotations

from typing import List

from .types import DiagramStep


def render_mermaid(steps: List[DiagramStep]) -> str:
    lines = ["flowchart TD", "    Start([Start])"]
    ids = ["Start"]
    for step in steps:
        node_id = sanitize(step.id)
        ids.append(node_id)
        label = step.label.replace('"', "'")
        if step.kind == "wait":
            lines.append(f'    {node_id}["👤 {label}"]')
        else:
            lines.append(f'    {node_id}["{label}"]')
    lines.append('    End(["Complete"])')
    ids.append("End")
    for i in range(len(ids) - 1):
        lines.append(f"    {ids[i]} --> {ids[i + 1]}")
    return "\n".join(lines) + "\n"


def sanitize(name: str) -> str:
    cleaned = "".join(c for c in name if c.isalnum())
    return cleaned if cleaned else "Node"
