# Senlinz.Permissions Technical Specification

Spec status: complete

## Summary

`Senlinz.Permissions` is a JSON-driven permission code generation system. It lets application teams define permissions once in `permission.json`, then use generated backend C# and generated frontend TypeScript or raw JSON consumption.

The project is separate from localization. Localization can be layered on top through label keys, but permission identities and authorization policies are stable application contracts.

## Goals

- Use `permission.json` as the single permission source of truth.
- Generate backend C# constants and catalogs.
- Support ASP.NET Core policy registration.
- Support frontend consumption through generated TypeScript or direct JSON import.
- Provide build-time validation and diagnostics.
- Keep generated output deterministic and friendly to source control diffing.

## Non-Goals

- Do not implement a full identity provider.
- Do not replace ASP.NET Core authorization.
- Do not treat frontend checks as security enforcement.
- Do not use localized text as permission ids.
- Do not generate frontend files from the Roslyn source generator directly.

## Package Architecture

### `Senlinz.Permissions.Abstractions`

Runtime package with shared models.

Target framework:

```text
netstandard2.0
```

Primary types:

```csharp
namespace Senlinz.Permissions;

public sealed class PermissionDefinition
{
    public PermissionDefinition(
        string id,
        string name,
        string? group = null,
        string? description = null,
        string? labelKey = null,
        string? descriptionKey = null,
        IReadOnlyList<string>? tags = null)
    {
        Id = id;
        Name = name;
        Group = group;
        Description = description;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
        Tags = tags ?? Array.Empty<string>();
    }

    public string Id { get; }
    public string Name { get; }
    public string? Group { get; }
    public string? Description { get; }
    public string? LabelKey { get; }
    public string? DescriptionKey { get; }
    public IReadOnlyList<string> Tags { get; }
}
```

### `Senlinz.Permissions`

Build package containing the C# incremental source generator.

Responsibilities:

- discover `permission.json`
- parse and validate JSON
- generate C# constants and catalog
- report diagnostics

### `Senlinz.Permissions.AspNetCore`

Runtime package for ASP.NET Core integration.

Responsibilities:

- register authorization policies from generated catalog
- support configurable claim type
- support configurable policy naming strategy

### `Senlinz.Permissions.Tool`

CLI package for frontend generation.

Responsibilities:

- parse the same `permission.json`
- generate TypeScript
- support CI validation
- optionally support schema generation

## JSON File

Default file name:

```text
permission.json
```

Default location:

```text
project root
```

Example:

```json
{
  "$schema": "https://schemas.senlinz.dev/permissions/v1.json",
  "version": 1,
  "namespace": "MyApp.Security",
  "className": "Permissions",
  "catalogClassName": "PermissionCatalog",
  "groups": [
    {
      "id": "users",
      "name": "Users",
      "labelKey": "permissions.groups.users"
    }
  ],
  "permissions": [
    {
      "id": "users.read",
      "name": "View users",
      "group": "users",
      "description": "Allows viewing user records.",
      "labelKey": "permissions.users.read.label",
      "descriptionKey": "permissions.users.read.description",
      "tags": ["frontend", "backend"]
    },
    {
      "id": "users.create",
      "name": "Create users",
      "group": "users",
      "description": "Allows creating user records.",
      "labelKey": "permissions.users.create.label",
      "descriptionKey": "permissions.users.create.description",
      "tags": ["frontend", "backend"]
    }
  ]
}
```

## JSON Schema Rules

Root fields:

| Field | Required | Type | Notes |
| --- | --- | --- | --- |
| `$schema` | no | string | JSON schema URI. |
| `version` | yes | integer | Schema version. Initial value is `1`. |
| `namespace` | no | string | Generated C# namespace override. |
| `className` | no | string | Generated permission constants class. Default `Permissions`. |
| `catalogClassName` | no | string | Generated catalog class. Default `PermissionCatalog`. |
| `groups` | no | array | Group metadata. |
| `permissions` | yes | array | Permission definitions. |

Group fields:

| Field | Required | Type | Notes |
| --- | --- | --- | --- |
| `id` | yes | string | Stable group id. |
| `name` | yes | string | Neutral display name. |
| `labelKey` | no | string | Localization key. |
| `description` | no | string | Neutral description. |
| `descriptionKey` | no | string | Localization key. |
| `order` | no | integer | UI ordering hint. |

Permission fields:

| Field | Required | Type | Notes |
| --- | --- | --- | --- |
| `id` | yes | string | Stable permission id and default policy name. |
| `name` | yes | string | Neutral display name. |
| `group` | no | string | Group id. |
| `description` | no | string | Neutral description. |
| `labelKey` | no | string | Localization key. |
| `descriptionKey` | no | string | Localization key. |
| `tags` | no | string array | Filtering metadata. |
| `order` | no | integer | UI ordering hint. |

Permission id format:

```text
^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$
```

Examples:

```text
users.read
users.create
orders.refund
system.audit.view
```

## MSBuild Properties

The generator package ships a props file:

```xml
<Project>
  <PropertyGroup>
    <SenlinzPermissionFile Condition="'$(SenlinzPermissionFile)' == ''">permission.json</SenlinzPermissionFile>
    <SenlinzPermissionClassName Condition="'$(SenlinzPermissionClassName)' == ''">Permissions</SenlinzPermissionClassName>
    <SenlinzPermissionCatalogClassName Condition="'$(SenlinzPermissionCatalogClassName)' == ''">PermissionCatalog</SenlinzPermissionCatalogClassName>
    <SenlinzPermissionStrict Condition="'$(SenlinzPermissionStrict)' == ''">true</SenlinzPermissionStrict>
  </PropertyGroup>

  <ItemGroup>
    <CompilerVisibleProperty Include="SenlinzPermissionFile" />
    <CompilerVisibleProperty Include="SenlinzPermissionNamespace" />
    <CompilerVisibleProperty Include="SenlinzPermissionClassName" />
    <CompilerVisibleProperty Include="SenlinzPermissionCatalogClassName" />
    <CompilerVisibleProperty Include="SenlinzPermissionStrict" />
  </ItemGroup>

  <ItemGroup>
    <AdditionalFiles Include="$(SenlinzPermissionFile)" Condition="Exists('$(SenlinzPermissionFile)')" />
  </ItemGroup>
</Project>
```

Consumer override:

```xml
<PropertyGroup>
  <SenlinzPermissionFile>Config/permission.json</SenlinzPermissionFile>
  <SenlinzPermissionNamespace>MyApp.Security</SenlinzPermissionNamespace>
</PropertyGroup>
```

## Generated C#

Generated constants:

```csharp
namespace MyApp.Security;

[System.CodeDom.Compiler.GeneratedCode("Senlinz.Permissions", "1.0.0.0")]
public static partial class Permissions
{
    public static class Users
    {
        public const string Read = "users.read";
        public const string Create = "users.create";
    }
}
```

Generated catalog:

```csharp
namespace MyApp.Security;

[System.CodeDom.Compiler.GeneratedCode("Senlinz.Permissions", "1.0.0.0")]
public static partial class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        new PermissionDefinition[]
        {
            new PermissionDefinition(
                "users.read",
                "View users",
                "users",
                "Allows viewing user records.",
                "permissions.users.read.label",
                "permissions.users.read.description",
                new[] { "frontend", "backend" }),

            new PermissionDefinition(
                "users.create",
                "Create users",
                "users",
                "Allows creating user records.",
                "permissions.users.create.label",
                "permissions.users.create.description",
                new[] { "frontend", "backend" }),
        };
}
```

Backend usage:

```csharp
[Authorize(Policy = Permissions.Users.Read)]
public IActionResult GetUsers()
{
    return Ok();
}
```

## ASP.NET Core Integration

Default extension:

```csharp
public static class AuthorizationOptionsExtensions
{
    public static AuthorizationOptions AddPermissionPolicies(
        this AuthorizationOptions options,
        IEnumerable<PermissionDefinition> permissions,
        string claimType = "permission")
    {
        foreach (var permission in permissions)
        {
            options.AddPolicy(permission.Id, policy =>
            {
                policy.RequireClaim(claimType, permission.Id);
            });
        }

        return options;
    }
}
```

Consumer usage:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies(PermissionCatalog.All);
});
```

## Generated TypeScript

The TypeScript generator is a CLI or MSBuild tool, not a Roslyn source generator.

Command:

```bash
dotnet senlinz-permissions generate-ts \
  --input permission.json \
  --output src/security/permissions.generated.ts
```

Generated output:

```ts
export const Permissions = {
  Users: {
    Read: "users.read",
    Create: "users.create"
  }
} as const;

export type PermissionId =
  | "users.read"
  | "users.create";

export const permissionCatalog = [
  {
    id: "users.read",
    name: "View users",
    group: "users",
    description: "Allows viewing user records.",
    labelKey: "permissions.users.read.label",
    descriptionKey: "permissions.users.read.description",
    tags: ["frontend", "backend"]
  },
  {
    id: "users.create",
    name: "Create users",
    group: "users",
    description: "Allows creating user records.",
    labelKey: "permissions.users.create.label",
    descriptionKey: "permissions.users.create.description",
    tags: ["frontend", "backend"]
  }
] as const;
```

Frontend usage:

```ts
import { Permissions, type PermissionId } from "./permissions.generated";

function can(userPermissions: readonly PermissionId[], permission: PermissionId) {
  return userPermissions.includes(permission);
}

can(currentUser.permissions, Permissions.Users.Read);
```

## Identifier Generation

Permission id segments become PascalCase identifiers.

Examples:

| Permission id | C# |
| --- | --- |
| `users.read` | `Permissions.Users.Read` |
| `users.create` | `Permissions.Users.Create` |
| `system.audit.view` | `Permissions.System.Audit.View` |

Identifier collisions are diagnostics.

Collision example:

```json
[
  { "id": "users.read", "name": "Read users" },
  { "id": "users-read", "name": "Read users legacy" }
]
```

The second id is invalid under the v1 id format. If future id formats allow it, the generator must still detect that both map to the same C# identifier path.

## Diagnostics

| Id | Severity | Description |
| --- | --- | --- |
| `SP001` | Error | `permission.json` is invalid JSON. |
| `SP002` | Error | Required root property is missing. |
| `SP003` | Error | Permission id is missing or invalid. |
| `SP004` | Error | Duplicate permission id. |
| `SP005` | Error | Generated C# identifier collision. |
| `SP006` | Warning | Permission references an unknown group. |
| `SP007` | Error | Generated namespace or class name is invalid. |
| `SP008` | Warning | Permission file is missing and strict mode is false. |
| `SP009` | Error | Permission file is missing and strict mode is true. |
| `SP010` | Error | Unsupported schema version. |

Diagnostics should include the JSON file path. Line and column should be included when practical.

## Incremental Generator Pipeline

The generator should use `IIncrementalGenerator`.

Pipeline:

1. Read analyzer config properties.
2. Filter `AdditionalTextsProvider` to the configured permission file.
3. Read file text.
4. Parse JSON into immutable models.
5. Validate model.
6. Combine with target namespace.
7. Emit sources or diagnostics.

The parser and emitter should be pure functions to keep tests simple.

## Determinism

Generated output must be deterministic:

- sort permissions by id unless explicit order is needed for UI catalogs
- sort groups by id unless explicit order is present
- preserve JSON order only when documented
- avoid timestamps
- avoid machine-specific paths in generated source

## Security Model

`permission.json` defines permission names and metadata. It does not grant permissions to users.

Actual grants come from identity data:

- claims
- roles mapped to permissions
- database grants
- external identity provider

Backend authorization must validate grants server-side. Frontend usage only controls navigation and UI visibility.

## Localization Integration

Permission ids are not localized.

Recommended pattern:

```json
{
  "id": "users.read",
  "name": "View users",
  "labelKey": "permissions.users.read.label",
  "descriptionKey": "permissions.users.read.description"
}
```

Localization files can provide display text:

```json
{
  "permissions": {
    "users": {
      "read": {
        "label": "View users",
        "description": "Allows viewing user records."
      }
    }
  }
}
```

## Testing Strategy

Parser tests:

- valid minimal file
- valid full file
- invalid JSON
- missing required fields
- duplicate ids
- unknown group

Generator tests:

- constants output
- catalog output
- namespace override
- class name override
- identifier collision
- missing file behavior

Tool tests:

- TypeScript constants output
- TypeScript union output
- deterministic ordering
- non-zero exit on invalid file

ASP.NET tests:

- policies are registered for all permissions
- default claim type is `permission`
- custom claim type works

## Versioning

Schema version starts at `1`.

Breaking JSON schema changes require a new schema version. The parser should reject unsupported future versions unless compatibility is explicitly implemented.

Permission id renames are application-level breaking changes. The package should not silently alias or migrate permission ids.

## Minimum Viable Release

Version `1.0.0` should include:

- abstractions package
- C# source generator
- JSON parser and validation
- generated constants
- generated catalog
- ASP.NET helper
- documentation
- tests

The TypeScript tool can ship in `1.1.0` if release scope needs to stay small, but the architecture should reserve it from the beginning.
