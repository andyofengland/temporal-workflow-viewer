from __future__ import annotations

import ast
from pathlib import Path
from typing import List

from .types import DiagramStep, ExtractedWorkflow, StepKind


def extract_workflows(source_dir: str | Path) -> List[ExtractedWorkflow]:
    """Scan source_dir for Temporal Python workflows using the ast module."""
    root = Path(source_dir)
    results: List[ExtractedWorkflow] = []
    for path in root.rglob("*.py"):
        if any(part in {"__pycache__", ".venv", "venv", "diagrams", "dist", "build"} for part in path.parts):
            continue
        try:
            text = path.read_text(encoding="utf-8")
            tree = ast.parse(text, filename=str(path))
        except (SyntaxError, UnicodeDecodeError):
            continue
        results.extend(_extract_from_module(tree, str(path)))
    return results


def _extract_from_module(tree: ast.AST, source_file: str) -> List[ExtractedWorkflow]:
    out: List[ExtractedWorkflow] = []
    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)) and _has_workflow_defn(node):
            out.append(
                ExtractedWorkflow(
                    name=node.name,
                    display_name=node.name,
                    steps=_collect_steps(node),
                    source_file=source_file,
                )
            )
    return out


def _has_workflow_defn(node: ast.FunctionDef | ast.AsyncFunctionDef) -> bool:
    for dec in node.decorator_list:
        name = _decorator_name(dec)
        if name in {"workflow.defn", "defn"} or name.endswith(".defn"):
            return True
    return False


def _decorator_name(dec: ast.AST) -> str:
    if isinstance(dec, ast.Call):
        return _expr_name(dec.func)
    return _expr_name(dec)


def _expr_name(expr: ast.AST) -> str:
    if isinstance(expr, ast.Name):
        return expr.id
    if isinstance(expr, ast.Attribute):
        base = _expr_name(expr.value)
        return f"{base}.{expr.attr}" if base else expr.attr
    return ""


def _collect_steps(node: ast.AST) -> List[DiagramStep]:
    steps: List[DiagramStep] = []
    counter = 0

    def add(label: str, kind: StepKind) -> None:
        nonlocal counter
        counter += 1
        sid = f"step{counter}_{_sanitize(label)}"
        steps.append(DiagramStep(id=sid, label=label, kind=kind))

    for child in ast.walk(node):
        if not isinstance(child, ast.Call):
            continue
        name = _expr_name(child.func)
        if name.endswith("execute_activity") or name == "workflow.execute_activity":
            add(_activity_label(child), "activity")
        elif name.endswith("execute_child_workflow") or name == "workflow.execute_child_workflow":
            add(_activity_label(child), "child")
        elif name == "workflow.sleep" or (name.endswith(".sleep") and "workflow" in name):
            add("Sleep", "wait")
        elif name.endswith("wait_condition") or name == "workflow.wait_condition":
            add("Wait condition", "wait")
        elif name == "asyncio.gather" or name.endswith(".gather"):
            add("Parallel", "parallel")
    return steps


def _activity_label(call: ast.Call) -> str:
    if not call.args:
        return "Activity"
    arg = call.args[0]
    if isinstance(arg, ast.Constant) and isinstance(arg.value, str):
        return arg.value
    if isinstance(arg, ast.Name):
        return arg.id
    if isinstance(arg, ast.Attribute):
        return arg.attr
    return "Activity"


def _sanitize(label: str) -> str:
    cleaned = "".join(c for c in label if c.isalnum())
    return cleaned[:40] if cleaned else "step"
