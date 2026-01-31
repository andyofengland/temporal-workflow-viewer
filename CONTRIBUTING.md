# Contributing to Temporal Dashboard

Thanks for your interest in contributing. This document gives a short overview of how to get involved.

## Getting started

- **Prerequisites**: .NET 10 SDK. See [README.md](README.md#prerequisites).
- **Setup**: Clone the repo, then run `dotnet build` and `dotnet test` from the solution root.
- **Running locally**: Start the API (`src/TemporalDashboard.Api`), then the Web app (`src/TemporalDashboard.Web`). See [README.md](README.md#option-2-local-development).
- **Codebase overview**: See [README.md](README.md#for-new-developers-working-with-and-extending-the-codebase).

## How to contribute

1. **Open an issue** (bug or feature) so we can align before larger changes.
2. **Fork** the repository and create a branch from `main`.
3. **Make your changes**: follow the structure in the README (diagramming in WorkflowDiagramming, API in Api, UI in Web).
4. **Run tests**: `dotnet test` must pass.
5. **Update docs**: If you add features or change behavior, update the README and any relevant guides (e.g. [WORKFLOW_ATTRIBUTES_GUIDE.md](WORKFLOW_ATTRIBUTES_GUIDE.md)).
6. **Open a pull request** against `main` with a clear description and reference to any related issue.

## Pull request guidelines

- Keep PRs focused; prefer several small PRs over one large one.
- Ensure CI (build and test) passes.
- New behavior should be covered by tests where practical (especially in the diagramming library).

## Code of conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Questions

Open a [GitHub issue](https://github.com/andyofengland/temporal-workflow-viewer/issues) for questions or discussion.
