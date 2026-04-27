# Senlinz.Permissions Technical Specification

Spec status: complete

## Summary

`Senlinz.Permissions` is a JSON-driven permission code generation system. It lets application teams define permissions once in `permission.json`, then use generated backend C# while frontend code consumes the same JSON directly.

The project is separate from localization. Localization can be layered on top through label keys or generated `LString` values, but permission codes and authorization policies are stable application contracts.

## Goals

- Use `permission.json` as the single permission source of truth.
- Generate backend C# constants and catalogs.
- Support ASP.NET Core policy registration.
- Support frontend consumption through direct JSON import.
- Provide build-time validation and diagnostics.
- Keep generated output deterministic and friendly to source control diffing.

## Non-Goals

- Do not implement a full identity provider.
- Do not replace ASP.NET Core authorization.
- Do not treat frontend checks as security enforcement.
- Do not use localized text as permission codes.
- Do not generate frontend TypeScript files.

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
        string code,
        string? name = null,
        string? group = null,
        IReadOnlyList<string>? requires = null,
        string? description = null,
        string? labelKey = null,
        string? descriptionKey = null,
        IReadOnlyList<string>? tags = null)
    {
        Code = code;
        Name = name;
        Group = group;
        Requires = requires ?? Array.Empty<string>();
        Description = description;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
        Tags = tags ?? Array.Empty<string>();
    }

    public string Code { get; }
    public string? Name { get; }
    public string? Group { get; }
    public IReadOnlyList<string> Requires { get; }
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
  "groups": [
    {
      "code": "users",
      "labelKey": "permissions.groups.users"
    }
  ],
  "permissions": [
    {
      "code": "users.read",
      "name": "View users",
      "group": "users",
      "description": "Allows viewing user records.",
      "labelKey": "permissions.users.read.label",
      "descriptionKey": "permissions.users.read.description",
      "tags": ["frontend", "backend"]
    },
    {
      "code": "users.create",
      "group": "users",
      "requires": ["users.read"]
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
| `namespace` | no | string | Generated C# namespace override. Used after `SenlinzPermissionNamespace` and before project defaults. |
| `className` | no | string | Generated permission constants class. Default `Permissions`. |
| `catalogClassName` | no | string | Generated catalog class. Default `PermissionCatalog`. |
| `groups` | no | array | Group metadata. |
| `permissions` | yes | array | Permission definitions. |

Group fields:

| Field | Required | Type | Notes |
| --- | --- | --- | --- |
| `code` | yes | string | Stable group code. |
| `name` | no | string | Neutral display name. |
| `labelKey` | no | string | Localization key. |
| `description` | no | string | Neutral description. |
| `descriptionKey` | no | string | Localization key. |
| `order` | no | integer | UI ordering hint. |

Permission fields:

| Field | Required | Type | Notes |
| --- | --- | --- | --- |
| `code` | yes | string | Stable permission code and default policy name. |
| `name` | no | string | Neutral display name. |
| `group` | no | string | Group code. |
| `requires` | no | string array | Dependent permission codes; granting this permission implies granting dependencies. |
| `description` | no | string | Neutral description. |
| `labelKey` | no | string | Localization key. |
| `descriptionKey` | no | string | Localization key. |
| `tags` | no | string array | Filtering metadata. |
| `order` | no | integer | UI ordering hint. |

Permission code format:

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

## Namespace Resolution

Generated C# namespace resolution order:

1. `SenlinzPermissionNamespace` MSBuild property.
2. `namespace` in `permission.json`.
3. project `RootNamespace`.
4. sanitized assembly name.

This allows the minimal setup to inherit the target project's namespace without writing namespace configuration in JSON.

## MSBuild Properties

The generator package ships a props file:

```xml
<Project>
  <PropertyGroup>
    <SenlinzPermissionFile Condition="'$(SenlinzPermissionFile)' == ''">permission.json</SenlinzPermissionFile>
    <SenlinzPermissionClassName Condition="'$(SenlinzPermissionClassName)' == ''">Permissions</SenlinzPermissionClassName>
    <SenlinzPermissionCatalogClassName Condition="'$(SenlinzPermissionCatalogClassName)' == ''">PermissionCatalog</SenlinzPermissionCatalogClassName>
    <SenlinzPermissionStrict Condition="'$(SenlinzPermissionStrict)' == ''">true</SenlinzPermissionStrict>
    <SenlinzPermissionGenerateLString Condition="'$(SenlinzPermissionGenerateLString)' == ''">false</SenlinzPermissionGenerateLString>
  </PropertyGroup>

  <ItemGroup>
    <CompilerVisibleProperty Include="SenlinzPermissionFile" />
    <CompilerVisibleProperty Include="SenlinzPermissionNamespace" />
    <CompilerVisibleProperty Include="SenlinzPermissionClassName" />
    <CompilerVisibleProperty Include="SenlinzPermissionCatalogClassName" />
    <CompilerVisibleProperty Include="SenlinzPermissionStrict" />
    <CompilerVisibleProperty Include="SenlinzPermissionGenerateLString" />
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
                name: "View users",
                group: "users",
                description: "Allows viewing user records.",
                labelKey: "permissions.users.read.label",
                descriptionKey: "permissions.users.read.description",
                tags: new[] { "frontend", "backend" }),

            new PermissionDefinition(
                "users.create",
                group: "users",
                requires: new[] { "users.read" }),
        };
}
```

Optional `LString` output is generated only when `SenlinzPermissionGenerateLString` is `true` and `Senlinz.Localization.Abstractions` is referenced:

```csharp
public static partial class PermissionL
{
    public static readonly LString UsersRead =
        new LString("permissions.users.read.label", "View users");
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
            options.AddPolicy(permission.Code, policy =>
            {
                policy.RequireClaim(claimType, permission.Code);
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

## Frontend Consumption

Frontend applications consume `permission.json` directly through their framework's JSON import support, a copied static asset, or an API response. This package does not generate frontend TypeScript artifacts.

## Identifier Generation

Permission code segments become PascalCase identifiers.

Examples:

| Permission code | C# |
| --- | --- |
| `users.read` | `Permissions.Users.Read` |
| `users.create` | `Permissions.Users.Create` |
| `system.audit.view` | `Permissions.System.Audit.View` |

Identifier collisions are diagnostics.

Collision example:

```json
[
  { "code": "users.read", "name": "Read users" },
  { "code": "users-read", "name": "Read users legacy" }
]
```

The second code is invalid under the v1 code format. If future code formats allow it, the generator must still detect that both map to the same C# identifier path.

## Diagnostics

| Id | Severity | Description |
| --- | --- | --- |
| `SP001` | Error | `permission.json` is invalid JSON. |
| `SP002` | Error | Required root property is missing. |
| `SP003` | Error | Permission code is missing or invalid. |
| `SP004` | Error | Duplicate permission code. |
| `SP005` | Error | Generated C# identifier collision. |
| `SP006` | Warning | Permission references an unknown group. |
| `SP007` | Error | Generated namespace or class name is invalid. |
| `SP008` | Warning | Permission file is missing and strict mode is false. |
| `SP009` | Error | Permission file is missing and strict mode is true. |
| `SP010` | Error | Unsupported schema version. |
| `SP011` | Error | `requires` references non-existent permission code. |
| `SP012` | Error | `requires` contains circular dependency. |

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

- sort permissions by code unless explicit order is needed for UI catalogs
- sort groups by code unless explicit order is present
- preserve JSON order only when documented
- avoid timestamps
- avoid machine-specific paths in generated source

## Security Model

`permission.json` defines permission codes and metadata. It does not grant permissions to users.

Actual grants come from identity data:

- claims
- roles mapped to permissions
- database grants
- external identity provider

Backend authorization must validate grants server-side. Frontend usage only controls navigation and UI visibility.

## Localization Integration

Permission codes are not localized.

Recommended pattern:

```json
{
  "code": "users.read",
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

When `SenlinzPermissionGenerateLString` is enabled, generated `LString` values should prefer `labelKey` and fall back to a conventional key derived from the permission code, such as `permissions.users.read.label`. The fallback text should use `name` when present, then the raw permission code.

## Testing Strategy

Parser tests:

- valid minimal file
- valid full file
- invalid JSON
- missing required fields
- duplicate codes
- unknown group
- invalid `requires` reference
- circular `requires` dependency

Generator tests:

- constants output
- catalog output
- namespace override
- class name override
- identifier collision
- missing file behavior

ASP.NET tests:

- policies are registered for all permissions
- default claim type is `permission`
- custom claim type works

## Versioning

Schema version starts at `1`.

Breaking JSON schema changes require a new schema version. The parser should reject unsupported future versions unless compatibility is explicitly implemented.

Permission code renames are application-level breaking changes. The package should not silently alias or migrate permission codes.

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
