using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Senlinz.Permissions;

public sealed class PermissionCatalog
{
    private readonly IReadOnlyDictionary<string, PermissionDefinition> permissionsByCode;

    public PermissionCatalog(
        IEnumerable<PermissionDefinition> permissions,
        IEnumerable<PermissionGroupDefinition>? groups = null)
    {
        if (permissions is null)
        {
            throw new ArgumentNullException(nameof(permissions));
        }

        Permissions = CopyPermissions(permissions, out permissionsByCode);
        Groups = CopyGroups(groups);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; }

    public IReadOnlyList<PermissionGroupDefinition> Groups { get; }

    public bool Contains(string code)
    {
        return code is not null && permissionsByCode.ContainsKey(code);
    }

    public PermissionDefinition? Find(string code)
    {
        if (code is null)
        {
            return null;
        }

        return permissionsByCode.TryGetValue(code, out var permission) ? permission : null;
    }

    public bool TryFind(string code, out PermissionDefinition? permission)
    {
        if (code is null)
        {
            permission = null;
            return false;
        }

        return permissionsByCode.TryGetValue(code, out permission);
    }

    public IReadOnlyList<PermissionDefinition> GetRequiredPermissions(string code)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        var result = new List<PermissionDefinition>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        AddRequiredPermissions(code, visited, result);
        return result.AsReadOnly();
    }

    private void AddRequiredPermissions(
        string code,
        HashSet<string> visited,
        List<PermissionDefinition> result)
    {
        if (!permissionsByCode.TryGetValue(code, out var permission))
        {
            return;
        }

        foreach (var requiredCode in permission.Requires)
        {
            if (!visited.Add(requiredCode))
            {
                continue;
            }

            if (permissionsByCode.TryGetValue(requiredCode, out var requiredPermission))
            {
                result.Add(requiredPermission);
                AddRequiredPermissions(requiredCode, visited, result);
            }
        }
    }

    private static IReadOnlyList<PermissionDefinition> CopyPermissions(
        IEnumerable<PermissionDefinition> permissions,
        out IReadOnlyDictionary<string, PermissionDefinition> permissionsByCode)
    {
        var list = new List<PermissionDefinition>();
        var map = new Dictionary<string, PermissionDefinition>(StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            if (permission is null)
            {
                throw new ArgumentException("Permission collection cannot contain null values.", nameof(permissions));
            }

            if (map.ContainsKey(permission.Code))
            {
                throw new ArgumentException($"Duplicate permission code '{permission.Code}'.", nameof(permissions));
            }

            list.Add(permission);
            map.Add(permission.Code, permission);
        }

        permissionsByCode = new ReadOnlyDictionary<string, PermissionDefinition>(map);
        return list.AsReadOnly();
    }

    private static IReadOnlyList<PermissionGroupDefinition> CopyGroups(IEnumerable<PermissionGroupDefinition>? groups)
    {
        if (groups is null)
        {
            return Array.Empty<PermissionGroupDefinition>();
        }

        var list = new List<PermissionGroupDefinition>();
        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in groups)
        {
            if (group is null)
            {
                throw new ArgumentException("Permission group collection cannot contain null values.", nameof(groups));
            }

            if (!codes.Add(group.Code))
            {
                throw new ArgumentException($"Duplicate permission group code '{group.Code}'.", nameof(groups));
            }

            list.Add(group);
        }

        return list.AsReadOnly();
    }
}
