<#
.SYNOPSIS
  Adds TemporalDashboard.WorkflowDiagramming.Roslyn.Build to the current (or specified) project.
  The package wires an MSBuild target that generates Mermaid diagrams from Temporal workflow
  C# source at build time (no diagramming attributes required).

.PARAMETER Project
  Path to a .csproj. If omitted, uses the single .csproj in the current directory.

.PARAMETER Version
  Optional package version.

.PARAMETER Source
  NuGet package source. Defaults to nuget.org.
#>
[CmdletBinding()]
param(
    [string]$Project = "",
    [string]$Version = "",
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Project)) {
    $projects = @(Get-ChildItem -Path . -Filter *.csproj -File)
    if ($projects.Count -ne 1) {
        Write-Error "Provide -Project, or run from a directory with exactly one .csproj."
    }
    $Project = $projects[0].FullName
}

$projPath = $Project
if (-not (Test-Path -LiteralPath $projPath)) {
    Write-Error "Project not found: $projPath"
}

$versionArg = @()
if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $versionArg = @("--version", $Version)
}

Write-Host "Adding TemporalDashboard.WorkflowDiagramming.Roslyn.Build (wires Roslyn source → Mermaid at build)..." -ForegroundColor Green
& dotnet add (Resolve-Path -LiteralPath $projPath) package TemporalDashboard.WorkflowDiagramming.Roslyn.Build --source $Source @versionArg
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Run 'dotnet build' — diagrams appear under bin/<Configuration>/<tfm>/diagrams/." -ForegroundColor Green
