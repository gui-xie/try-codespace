# Senlinz.Permissions

`Senlinz.Permissions` generates backend C# permission constants and catalogs from a single `permission.json` file. Frontend code can consume the same JSON directly, while backend authorization remains enforced server-side.

## Packages

- `Senlinz.Permissions.Abstractions`: runtime models such as `PermissionDefinition`, `PermissionGroupDefinition`, and `PermissionCatalog`.
- `Senlinz.Permissions`: source generator package that discovers `permission.json`, validates it, and emits generated C#.
- `Senlinz.Permissions.AspNetCore`: helpers for registering generated permissions as ASP.NET Core authorization policies.

## permission.json

```json
{
  "$schema": "https://schemas.senlinz.dev/permissions/v1.json",
  "version": 1,
  "groups": [
    { "code": "users", "name": "Users" }
  ],
  "permissions": [
    {
      "code": "users.read",
      "name": "View users",
      "group": "users",
      "labelKey": "permissions.users.read.label"
    },
    {
      "code": "users.create",
      "group": "users",
      "requires": ["users.read"]
    }
  ]
}
```

Generated constants can be used in backend authorization attributes:

```csharp
[Authorize(Policy = Permissions.Users.Read)]
public IActionResult GetUsers()
{
    return Ok();
}
```

Generated catalogs can be registered with ASP.NET Core:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies(PermissionCatalog.All);
});
```

## MSBuild Options

- `SenlinzPermissionFile`: permission JSON path. Default: `permission.json`.
- `SenlinzPermissionNamespace`: generated namespace override.
- `SenlinzPermissionClassName`: constants class name. Default: `Permissions`.
- `SenlinzPermissionCatalogClassName`: catalog class name. Default: `PermissionCatalog`.
- `SenlinzPermissionStrict`: missing file is an error when `true`. Default: `true`.
- `SenlinzPermissionGenerateLString`: generate localization accessors when localization abstractions are referenced. Default: `false`.

## Development

```bash
dotnet build -m:1 Senlinz.Permissions.sln
dotnet run --project tests/Senlinz.Permissions.Tests/Senlinz.Permissions.Tests.csproj
dotnet pack src/Senlinz.Permissions/Senlinz.Permissions.csproj
```

The test runner is dependency-free so the repository can validate without downloading test packages.
