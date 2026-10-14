# Agent instructions

These instructions apply to every change in this repository.

## Tests

Run tests after each change, before considering the work done:

```bash
dotnet test
```

Run that from the solution root. If a change is limited to one project, you may also run the matching test project, but still finish with a full `dotnet test`.

If tests fail, fix the failure before moving on. Do not leave a change in a state where the suite does not pass.

## Breaking changes

Call out breaking changes explicitly whenever a change alters existing behavior that callers, NuGet consumers, or the Web/API contract rely on. This includes:

- Public API, attribute, or model changes in the WorkflowDiagramming packages
- MSBuild task inputs, outputs, or generated diagram format
- HTTP routes, request/response shapes, or upload/diagram package format
- Removed or renamed configuration, files, or user-facing flows

When a change is breaking:

1. Say so in the summary of the change, in plain language: what broke, who is affected, and what they must do.
2. Add a `### Breaking` entry under `[Unreleased]` in [CHANGELOG.md](CHANGELOG.md).
3. Update the README and any affected guide (for example [WORKFLOW_ATTRIBUTES_GUIDE.md](WORKFLOW_ATTRIBUTES_GUIDE.md)) in the same change.

If a change is compatible, say that it is not breaking when the risk is easy to miss (public types, package output, API responses, or diagram format).
