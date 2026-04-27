using System;
using Senlinz.Permissions;

namespace Senlinz.Permissions.AspNetCore;

public sealed class PermissionAuthorizationOptions
{
    public string ClaimType { get; set; } = PermissionAuthorizationDefaults.ClaimType;

    public string PolicyPrefix { get; set; } = string.Empty;

    public Func<PermissionDefinition, string>? PolicyNameSelector { get; set; }

    internal string GetPolicyName(PermissionDefinition permission)
    {
        var policyName = PolicyNameSelector is null ? permission.Code : PolicyNameSelector(permission);
        return string.IsNullOrEmpty(PolicyPrefix) ? policyName : PolicyPrefix + policyName;
    }
}
