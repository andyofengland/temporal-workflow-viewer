from __future__ import annotations

import json
import platform
import zipfile
from dataclasses import asdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

from .extract import extract_workflows
from .mermaid import render_mermaid, sanitize
from .types import WorkflowDiagramsMetadata, WorkflowEntry

GENERATOR = "workflow-diagramming-python/0.1.0"


def write_diagrams(
    source_dir: str | Path,
    out_dir: str | Path,
    *,
    package_name: Optional[str] = None,
    package_version: str = "0.0.0",
    create_zip: bool = False,
) -> WorkflowDiagramsMetadata:
    source = Path(source_dir).resolve()
    out = Path(out_dir).resolve()
    out.mkdir(parents=True, exist_ok=True)

    workflows = extract_workflows(source)
    pkg = (package_name or source.name or "workflows").strip()

    meta = WorkflowDiagramsMetadata(
        packageName=pkg,
        packageVersion=package_version,
        packagePath=str(source),
        language="Python",
        targetFramework=f"python{platform.python_version_tuple()[0]}.{platform.python_version_tuple()[1]}",
        buildDateUtc=datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        generator=GENERATOR,
        workflows=[],
    )

    used: set[str] = set()
    for wf in workflows:
        base = sanitize(wf.name) or "Workflow"
        file_base = base
        n = 2
        while file_base.lower() in used:
            file_base = f"{base}{n}"
            n += 1
        used.add(file_base.lower())
        diagram_file = f"{file_base}.mermaid"
        (out / diagram_file).write_text(render_mermaid(wf.steps), encoding="utf-8")
        meta.workflows.append(
            WorkflowEntry(name=wf.name, displayName=wf.display_name, diagramFile=diagram_file)
        )

    meta_path = out / "workflow-diagrams-metadata.json"
    meta_path.write_text(json.dumps(asdict(meta), indent=2) + "\n", encoding="utf-8")

    if create_zip:
        zip_path = out / "workflow-diagrams.zip"
        with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
            zf.write(meta_path, arcname="workflow-diagrams-metadata.json")
            for w in meta.workflows:
                zf.write(out / w.diagramFile, arcname=w.diagramFile)

    return meta
