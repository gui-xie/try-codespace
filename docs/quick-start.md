# Quick Start

1. Add `permission.json` to the consuming project root.
2. Reference `Senlinz.Permissions` for generation and `Senlinz.Permissions.Abstractions` for runtime catalog models.
3. Use generated constants from the target project's root namespace.
4. Register generated permissions with ASP.NET Core when server authorization policies are needed.

```xml
<ItemGroup>
  <PackageReference Include="Senlinz.Permissions" Version="1.0.0" PrivateAssets="all" />
  <PackageReference Include="Senlinz.Permissions.Abstractions" Version="1.0.0" />
  <PackageReference Include="Senlinz.Permissions.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

```json
{
  "version": 1,
  "permissions": [
    { "code": "users.read", "name": "View users" },
    { "code": "users.create", "requires": ["users.read"] }
  ]
}
```

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies(PermissionCatalog.All);
});
```

Permission codes are stable authorization contract values. Rename them only as a breaking application change.
