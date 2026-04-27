# ASP.NET Core Integration

`Senlinz.Permissions.AspNetCore` adds `AuthorizationOptions.AddPermissionPolicies`.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies(PermissionCatalog.All);
});
```

The default policy name is the permission code, and the default claim type is `permission`.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies(
        PermissionCatalog.All,
        permissionOptions =>
        {
            permissionOptions.ClaimType = "scope";
            permissionOptions.PolicyPrefix = "perm:";
        });
});
```

This package registers policies only. It does not grant permissions to users; grants still come from claims, roles, database grants, or an identity provider.
