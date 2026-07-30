#!/usr/bin/env bash
# Adds TemporalDashboard.WorkflowDiagramming.Roslyn.Build to the current (or specified) project.
# The package wires an MSBuild target that generates Mermaid diagrams from Temporal workflow
# C# source at build time (no diagramming attributes required).
#
# Usage:
#   ./install-workflow-diagramming-roslyn-build.sh
#   ./install-workflow-diagramming-roslyn-build.sh [path/to/Project.csproj] [version]
#
# Env:
#   NUGET_SOURCE  Package source (default: nuget.org)

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

echo "Adding TemporalDashboard.WorkflowDiagramming.Roslyn.Build (wires Roslyn source → Mermaid at build)..."
dotnet add "$PROJ_PATH" package TemporalDashboard.WorkflowDiagramming.Roslyn.Build --source "$SOURCE" "${VERSION_ARGS[@]}"

echo "Done. Run 'dotnet build' — diagrams appear under bin/<Configuration>/<tfm>/diagrams/."
