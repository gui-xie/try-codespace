using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Senlinz.Permissions;

namespace Senlinz.Permissions.AspNetCore;

public static class AuthorizationOptionsExtensions
{
    public static AuthorizationOptions AddPermissionPolicies(
        this AuthorizationOptions options,
        IEnumerable<PermissionDefinition> permissions,
        string claimType = PermissionAuthorizationDefaults.ClaimType)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (permissions is null)
        {
            throw new ArgumentNullException(nameof(permissions));
        }

        return options.AddPermissionPolicies(
            permissions,
            permissionOptions => permissionOptions.ClaimType = claimType);
    }

    public static AuthorizationOptions AddPermissionPolicies(
        this AuthorizationOptions options,
        IEnumerable<PermissionDefinition> permissions,
        Action<PermissionAuthorizationOptions> configure)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (permissions is null)
        {
            throw new ArgumentNullException(nameof(permissions));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var permissionOptions = new PermissionAuthorizationOptions();
        configure(permissionOptions);

        if (string.IsNullOrWhiteSpace(permissionOptions.ClaimType))
        {
            throw new ArgumentException("Permission claim type cannot be empty.", nameof(configure));
        }

        foreach (var permission in permissions)
        {
            if (permission is null)
            {
                throw new ArgumentException("Permission collection cannot contain null values.", nameof(permissions));
            }

            var policyName = permissionOptions.GetPolicyName(permission);
            if (string.IsNullOrWhiteSpace(policyName))
            {
                throw new ArgumentException($"Policy name for permission '{permission.Code}' cannot be empty.", nameof(configure));
            }

            options.AddPolicy(policyName, policy =>
            {
                policy.RequireClaim(permissionOptions.ClaimType, permission.Code);
            });
        }

        return options;
    }
}
