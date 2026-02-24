#Requires -Version 5.1
<#
.SYNOPSIS
  Adds TemporalDashboard.WorkflowDiagramming and TemporalDashboard.WorkflowDiagramming.Build
  to the current (or specified) project. The Build package wires the MSBuild task automatically.

.DESCRIPTION
  When the packages are added:
  - TemporalDashboard.WorkflowDiagramming provides attributes and diagram generation for your workflows.
  - TemporalDashboard.WorkflowDiagramming.Build registers an MSBuild target that runs after Build
    and generates .mermaid files to $(OutputPath)diagrams. No manual Import is required.

.PARAMETER Project
  Path to the .csproj file. Defaults to the first .csproj in the current directory.

.PARAMETER Version
  Package version to install (e.g. 1.0.0). Defaults to latest.

.PARAMETER Source
  NuGet package source. Defaults to nuget.org.

.EXAMPLE
  .\install-workflow-diagramming-build.ps1
  .\install-workflow-diagramming-build.ps1 -Project .\src\MyWorkflows\MyWorkflows.csproj -Version 1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Project = "",
    [string]$Version = "",
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"

function Find-Project {
    if ($Project -ne "") {
        $p = $Project
        if (-not [System.IO.Path]::IsPathRooted($p)) {
            $p = Join-Path (Get-Location) $p
        }
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
            Write-Error "Project file not found: $p"
        }
        return $p
    }
    $here = Get-Location
    $projs = @(Get-ChildItem -Path $here -Filter "*.csproj" -File -ErrorAction SilentlyContinue)
    if ($projs.Count -eq 0) {
        Write-Error "No .csproj found in current directory. Specify -Project path or run from a project directory."
    }
    if ($projs.Count -gt 1) {
        Write-Warning "Multiple .csproj files found; using: $($projs[0].FullName)"
    }
    return $projs[0].FullName
}

$projPath = Find-Project
$projDir = [System.IO.Path]::GetDirectoryName($projPath)
$projName = [System.IO.Path]::GetFileName($projPath)

Write-Host "Project: $projPath" -ForegroundColor Cyan

$versionArg = @()
if ($Version -ne "") {
    $versionArg = @("--version", $Version)
}

Write-Host "Adding TemporalDashboard.WorkflowDiagramming..." -ForegroundColor Green
& dotnet add (Resolve-Path -LiteralPath $projPath) package TemporalDashboard.WorkflowDiagramming --source $Source @versionArg
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Adding TemporalDashboard.WorkflowDiagramming.Build (wires build task automatically)..." -ForegroundColor Green
& dotnet add (Resolve-Path -LiteralPath $projPath) package TemporalDashboard.WorkflowDiagramming.Build --source $Source @versionArg
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Done. The build task will run after each build and write .mermaid files to:" -ForegroundColor Green
Write-Host "  bin/<Configuration>/<TargetFramework>/diagrams/" -ForegroundColor White
Write-Host ""
Write-Host "Annotate your workflow types with [WorkflowDiagram], [WorkflowStep], etc. See:" -ForegroundColor Green
Write-Host "  https://github.com/andynightingale/temporal-workflow-viewer/blob/main/WORKFLOW_ATTRIBUTES_GUIDE.md" -ForegroundColor White
