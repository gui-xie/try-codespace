# Senlinz.Permissions Project Plan

Planning status: complete

Implementation status: initial 1.0 package family implemented

## Objective

Create a new permissions package family that uses `permission.json` as the single source of truth for both frontend and backend permission definitions.

The backend must not require a manually maintained C# permissions file. Backend constants, catalogs, authorization helpers, and validation diagnostics are generated from JSON. The frontend should consume the same JSON directly.

## Guiding Principles

- `permission.json` owns stable permission codes and permission metadata.
- Generated C# owns backend type safety.
- Permission codes are not localized text.
- Frontend permission checks are only UI affordances; backend authorization remains authoritative.
- The project should be independent from `Senlinz.Localization`, but may reuse its generator patterns.

## Proposed Repository Layout

```text
src/
  Senlinz.Permissions/
  Senlinz.Permissions.Abstractions/
  Senlinz.Permissions.AspNetCore/
tests/
  Senlinz.Permissions.Tests/
docs/
```

## Milestone 1: Core Contracts

Deliverables:

- Create `Senlinz.Permissions.Abstractions`.
- Define immutable runtime models:
  - `PermissionDefinition`
  - `PermissionGroupDefinition`
  - `PermissionCatalog`
  - optional `PermissionMetadata`
- Require only `code` for each permission; keep `name`, `description`, `group`, `requires`, and localization fields optional.
- Keep abstractions free from Roslyn and ASP.NET dependencies.
- Target `netstandard2.0` unless a specific API requires a higher target.

Acceptance criteria:

- A consumer can reference the abstractions package without loading the generator.
- Runtime contracts are serializable enough for API responses.
- Contracts do not require frontend-specific concepts.
- A minimal permission entry such as `{ "code": "users.read" }` is valid.

## Milestone 2: JSON Schema and Parser

Deliverables:

- Define `permission.json` v1 schema.
- Implement parser using `System.Text.Json`.
- Support deterministic ordering.
- Support strict validation with useful diagnostics.
- Add schema documentation.

Acceptance criteria:

- Duplicate permission codes are detected.
- Duplicate generated C# identifiers are detected.
- Missing required fields, especially `code`, are detected.
- Invalid permission code format is detected.
- Invalid or circular `requires` dependencies are detected.
- Parser can be tested independently from Roslyn.

## Milestone 3: C# Source Generator

Deliverables:

- Create `Senlinz.Permissions` source generator package.
- Read `permission.json` from `AdditionalFiles`.
- Expose MSBuild properties:
  - `SenlinzPermissionFile`
  - `SenlinzPermissionNamespace`
  - `SenlinzPermissionClassName`
  - `SenlinzPermissionCatalogClassName`
  - `SenlinzPermissionStrict`
  - `SenlinzPermissionGenerateLString`
- Resolve the generated namespace in this order:
  - `SenlinzPermissionNamespace`
  - `namespace` in `permission.json`
  - project `RootNamespace`
  - sanitized assembly name
- Emit generated C#:
  - `Permissions.g.cs`
  - `PermissionCatalog.g.cs`
  - optional nested group classes
  - optional `LString` accessors when localization integration is enabled
- Report diagnostics for invalid input.

Acceptance criteria:

- Consumer code can use `[Authorize(Policy = Permissions.Users.Read)]`.
- Consumer code can enumerate `PermissionCatalog.All`.
- Consumer projects do not need to set a namespace when their `RootNamespace` is suitable.
- No manually maintained backend permission C# file is needed.
- Incremental generator output is stable across builds.

## Milestone 4: ASP.NET Core Integration

Deliverables:

- Create `Senlinz.Permissions.AspNetCore`.
- Add authorization registration helpers.
- Support basic claim-based policies.
- Allow claim type configuration.
- Allow policy prefix configuration if needed.

Acceptance criteria:

- Consumer can register all generated permissions in one call.
- Default behavior uses permission code as policy name.
- Package does not require the generator package at runtime.

## Milestone 5: Packaging and Build Integration

Deliverables:

- Add NuGet package metadata.
- Pack generator as analyzer and runtime support as lib.
- Add `.props` for default `permission.json` discovery.

Acceptance criteria:

- Consumer install path is simple:

```xml
<PackageReference Include="Senlinz.Permissions" Version="1.0.0" />
```

- Default `permission.json` in project root is discovered.
- Build diagnostics point at the JSON file where possible.

## Milestone 6: Tests

Deliverables:

- Parser unit tests.
- Generator snapshot tests.
- Diagnostic tests.
- ASP.NET helper tests.

Acceptance criteria:

- Generated code compiles in a sample project.
- Invalid catalogs produce expected diagnostic ids.
- Generated output is deterministic.

## Milestone 7: Documentation and Samples

Deliverables:

- Quick start.
- JSON schema reference.
- Backend usage sample.
- Frontend usage sample.
- ASP.NET Core integration sample.
- Migration guidance from manually maintained C# permission classes.

Acceptance criteria:

- A new user can add one permission in JSON and use it in backend and frontend.
- Documentation clearly explains why permission codes are stable and non-localized.

## Recommended First Implementation Slice

Build the smallest useful version first:

1. `Senlinz.Permissions.Abstractions`
2. JSON parser
3. C# generator with constants and `PermissionCatalog.All`
4. duplicate-code and invalid-json diagnostics
5. one sample app or test project

Defer until after the core generator works:

- ASP.NET policy provider
- schema hosting
- optional `LString` localization integration
- advanced permission inheritance

## Risks

- If the JSON schema mixes authorization and UI layout too tightly, backend and frontend changes will become coupled in unhealthy ways.
- If localized display names live directly in `permission.json`, permission metadata becomes culture-specific. Prefer stable keys, optional neutral display names, or localization keys.
- Permission codes must be treated as public contract values. Renaming them is a breaking change.

## Completion Definition

The project is complete when:

- `permission.json` is the only manually edited permission source.
- Backend C# constants and catalogs are generated at build time.
- Backend authorization can register generated permissions.
- Frontend can consume permission.json directly.
- Invalid permission catalogs fail with actionable diagnostics.
- Packages and docs are ready for NuGet consumption.
