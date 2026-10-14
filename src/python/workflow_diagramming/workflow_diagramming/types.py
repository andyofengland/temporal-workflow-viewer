from __future__ import annotations

from dataclasses import dataclass, field
from typing import List, Literal, Optional


StepKind = Literal["activity", "wait", "child", "parallel"]


@dataclass
class DiagramStep:
    id: str
    label: str
    kind: StepKind


@dataclass
class ExtractedWorkflow:
    name: str
    display_name: str
    steps: List[DiagramStep] = field(default_factory=list)
    source_file: str = ""


@dataclass
class WorkflowEntry:
    name: str
    displayName: Optional[str]
    diagramFile: str


@dataclass
class WorkflowDiagramsMetadata:
    packageName: str
    language: str
    buildDateUtc: str
    generator: str
    workflows: List[WorkflowEntry] = field(default_factory=list)
    packageVersion: str = "0.0.0"
    packagePath: Optional[str] = None
    targetFramework: Optional[str] = None
