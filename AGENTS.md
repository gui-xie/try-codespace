# Repository Guidelines

## Project Structure & Module Organization

This repository currently contains planning documents for the proposed `Senlinz.Permissions` project:

- `PERMISSIONS_PROJECT_PLAN.md` describes milestones and delivery order.
- `PERMISSIONS_PROJECT_SPEC.md` defines the technical contract, JSON schema, generated C#, and TypeScript output.
- `LICENSE` contains repository licensing.

When implementation begins, follow the planned layout:

```text
src/
  Senlinz.Permissions/
  Senlinz.Permissions.Abstractions/
  Senlinz.Permissions.AspNetCore/
  Senlinz.Permissions.Tool/
tests/
  Senlinz.Permissions.Tests/
docs/
```

Keep source, tests, and docs separated. Do not place generated output in source folders unless it is intentionally committed as a fixture or snapshot.

## Build, Test, and Development Commands

There is no buildable project yet. Once the .NET solution exists, use:

```bash
dotnet build
dotnet test
dotnet pack
```

`dotnet build` compiles all projects, `dotnet test` runs the test suite, and `dotnet pack` validates NuGet packaging. For documentation-only changes, run `git diff --check` before committing to catch whitespace errors.

## Coding Style & Naming Conventions

Use C# conventions for future implementation:

- 4-space indentation.
- PascalCase for public types, methods, and properties.
- camelCase for locals and parameters.
- `Async` suffix for asynchronous methods.
- Diagnostic ids should use the `SP###` pattern, for example `SP001`.

Keep generated code deterministic: stable ordering, no timestamps, and no machine-specific paths.

## Testing Guidelines

Use focused unit tests for parser, generator, CLI, and ASP.NET integration behavior. Test names should describe behavior, for example:

```csharp
Generates_constants_for_valid_permission_file
Reports_duplicate_permission_ids
```

Generator tests should verify both diagnostics and generated source. Prefer snapshot-style assertions only when output is intentionally stable and readable.

## Commit & Pull Request Guidelines

Current history uses short imperative commit messages, for example `Add permissions project plan and spec`. Keep that style:

```text
Add permission JSON parser
Report duplicate permission ids
Document TypeScript output
```

Pull requests should include a concise summary, validation performed, and any linked issue. For generated-code changes, include before/after examples or snapshots. For user-facing behavior changes, update the relevant plan, spec, or docs in the same PR.

## Agent-Specific Instructions

Do not overwrite unrelated local changes. The `.codex` file may be untracked local state and should be left alone unless explicitly requested. Keep documentation concise and aligned with `PERMISSIONS_PROJECT_SPEC.md`.
