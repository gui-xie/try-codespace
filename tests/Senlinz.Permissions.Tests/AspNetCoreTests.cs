using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Senlinz.Permissions;
using Senlinz.Permissions.AspNetCore;
using Xunit;

namespace Senlinz.Permissions.Tests;

public sealed class AspNetCoreTests
{
    [Fact]
    public void Registers_policies_for_all_permissions()
    {
        var options = new AuthorizationOptions();
        var permissions = new[]
        {
            new PermissionDefinition("users.read"),
            new PermissionDefinition("users.create")
        };

        options.AddPermissionPolicies(permissions);

        Assert.NotNull(options.GetPolicy("users.read"));
        Assert.NotNull(options.GetPolicy("users.create"));
    }

    [Fact]
    public void Uses_default_permission_claim_type()
    {
        var options = new AuthorizationOptions();

        options.AddPermissionPolicies(new[] { new PermissionDefinition("users.read") });

        var requirement = Assert.Single(options.GetPolicy("users.read")!.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal("permission", requirement.ClaimType);
        Assert.Contains("users.read", requirement.AllowedValues!);
    }

    [Fact]
    public void Supports_custom_claim_type_and_policy_prefix()
    {
        var options = new AuthorizationOptions();

        options.AddPermissionPolicies(
            new[] { new PermissionDefinition("users.read") },
            permissionOptions =>
            {
                permissionOptions.ClaimType = "scope";
                permissionOptions.PolicyPrefix = "perm:";
            });

        var requirement = Assert.Single(options.GetPolicy("perm:users.read")!.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal("scope", requirement.ClaimType);
        Assert.Contains("users.read", requirement.AllowedValues!);
    }
}
