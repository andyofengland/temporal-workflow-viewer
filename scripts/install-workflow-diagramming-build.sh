#!/usr/bin/env bash
# Adds TemporalDashboard.WorkflowDiagramming and TemporalDashboard.WorkflowDiagramming.Build
# to the current (or specified) project. The Build package wires the MSBuild task automatically.
#
# Usage:
#   ./install-workflow-diagramming-build.sh
#   ./install-workflow-diagramming-build.sh [path/to/Project.csproj] [version]
#
# When the packages are added, the Build package's .targets are imported by NuGet automatically,
# so diagram generation runs after each build and writes .mermaid files to $(OutputPath)diagrams.

set -e

PROJECT="${1:-}"
VERSION="${2:-}"
SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

find_project() {
  if [[ -n "$PROJECT" ]]; then
    if [[ -f "$PROJECT" ]]; then
      echo "$PROJECT"
      return
    fi
    if [[ -f "$(pwd)/$PROJECT" ]]; then
      echo "$(pwd)/$PROJECT"
      return
    fi
    echo "Project file not found: $PROJECT" >&2
    exit 1
  fi
  local projs
  projs=( ./*.csproj )
  if [[ ! -f "${projs[0]}" ]]; then
    echo "No .csproj found in current directory. Pass project path as first argument or run from a project directory." >&2
    exit 1
  fi
  if [[ ${#projs[@]} -gt 1 ]]; then
    echo "Multiple .csproj files; using: ${projs[0]}" >&2
  fi
  echo "${projs[0]}"
}

PROJ_PATH="$(find_project)"
PROJ_PATH="$(cd "$(dirname "$PROJ_PATH")" && pwd)/$(basename "$PROJ_PATH")"

echo "Project: $PROJ_PATH"

VERSION_ARGS=()
[[ -n "$VERSION" ]] && VERSION_ARGS=( --version "$VERSION" )

echo "Adding TemporalDashboard.WorkflowDiagramming..."
dotnet add "$PROJ_PATH" package TemporalDashboard.WorkflowDiagramming --source "$SOURCE" "${VERSION_ARGS[@]}"

echo "Adding TemporalDashboard.WorkflowDiagramming.Build (wires build task automatically)..."
dotnet add "$PROJ_PATH" package TemporalDashboard.WorkflowDiagramming.Build --source "$SOURCE" "${VERSION_ARGS[@]}"

echo ""
echo "Done. The build task will run after each build and write .mermaid files to:"
echo "  bin/<Configuration>/<TargetFramework>/diagrams/"
echo ""
echo "Annotate your workflow types with [WorkflowDiagram], [WorkflowStep], etc. See:"
echo "  https://github.com/andyofengland/temporal-workflow-viewer/blob/main/WORKFLOW_ATTRIBUTES_GUIDE.md"
