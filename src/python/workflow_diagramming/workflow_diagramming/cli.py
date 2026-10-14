from __future__ import annotations

import argparse
import sys
from pathlib import Path

from .write import write_diagrams


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        prog="workflow-diagramming",
        description="Extract Mermaid diagrams from Temporal Python workflows",
    )
    sub = parser.add_subparsers(dest="command")

    extract = sub.add_parser("extract", help="Scan a source directory and write diagrams")
    extract.add_argument("source_dir", help="Directory containing workflow .py files")
    extract.add_argument("--out", default=None, help="Output directory (default: <source>/diagrams)")
    extract.add_argument("--package-name", default=None, help="packageName in metadata")
    extract.add_argument("--package-version", default="0.0.0", help="packageVersion in metadata")
    extract.add_argument("--zip", action="store_true", help="Also write workflow-diagrams.zip")

    args = parser.parse_args(argv)
    if args.command != "extract":
        parser.print_help()
        return 1

    source = Path(args.source_dir)
    out = Path(args.out) if args.out else source / "diagrams"
    meta = write_diagrams(
        source,
        out,
        package_name=args.package_name,
        package_version=args.package_version,
        create_zip=args.zip,
    )
    print(f"Wrote {len(meta.workflows)} workflow diagram(s) to {out.resolve()}")
    for w in meta.workflows:
        print(f"  - {w.name} -> {w.diagramFile}")
    if args.zip:
        print(f"Zip: {out.resolve() / 'workflow-diagrams.zip'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
