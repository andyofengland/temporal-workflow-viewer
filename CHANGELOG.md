# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) where applicable.

## [Unreleased]

### Added

- Initial open-source release: Temporal Dashboard for discovering and visualizing Temporal .NET workflows.
- Upload zip of workflow DLLs, discover workflows, generate Mermaid diagrams from diagramming attributes.
- Blazor Web UI: Home, Upload, View Workflows, Learn, Annotations Guide, Mermaid to Workflow wizard.
- REST API: upload, list workflows, get diagrams per DLL.
- WorkflowDiagramming library: attributes and Mermaid generator.
- WorkflowDiagramming.Roslyn library: source-based diagram extraction via Roslyn (no diagramming attributes); published to NuGet alongside the core and Build packages.
- WorkflowDiagramming.Roslyn.Build: MSBuild task that generates Mermaid diagrams from workflow C# source at build time (add package, no app code).
- Docs and Web UI (Diagramming Guide, Home, Learn, Upload, Wizard) updated for attributes vs Roslyn paths.
- Docker Compose setup; documentation (README, DOCKER.md, WORKFLOW_ATTRIBUTES_GUIDE.md).
- CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md, LICENSE (MIT).
