using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Senlinz.Permissions.Generation;

public static class PermissionJsonParser
{
    public static PermissionParseResult Parse(string json, string? path = null)
    {
        var diagnostics = new List<PermissionValidationDiagnostic>();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP001",
                PermissionDiagnosticSeverity.Error,
                $"permission.json is invalid JSON: {exception.Message}",
                filePath: path,
                line: ToOneBasedInt(exception.LineNumber),
                column: ToOneBasedInt(exception.BytePositionInLine)));

            return new PermissionParseResult(null, diagnostics);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP001",
                    PermissionDiagnosticSeverity.Error,
                    "permission.json root must be a JSON object.",
                    path));

                return new PermissionParseResult(null, diagnostics);
            }

            var root = document.RootElement;
            var version = ReadRequiredVersion(root, diagnostics, path);
            var generatedNamespace = ReadOptionalString(root, "namespace", "$.namespace", diagnostics, path);
            var className = ReadOptionalString(root, "className", "$.className", diagnostics, path);
            var catalogClassName = ReadOptionalString(root, "catalogClassName", "$.catalogClassName", diagnostics, path);
            var groups = ReadGroups(root, diagnostics, path);
            var permissions = ReadPermissions(root, diagnostics, path);

            ValidateGroups(groups, diagnostics, path);
            ValidatePermissions(groups, permissions, diagnostics, path);

            var sortedGroups = groups
                .OrderBy(static group => group.Order ?? int.MaxValue)
                .ThenBy(static group => group.Code, StringComparer.Ordinal)
                .ToArray();

            var sortedPermissions = permissions
                .OrderBy(static permission => permission.Order ?? int.MaxValue)
                .ThenBy(static permission => permission.Code, StringComparer.Ordinal)
                .ToArray();

            var permissionDocument = new PermissionDocument(
                version,
                generatedNamespace,
                className,
                catalogClassName,
                sortedGroups,
                sortedPermissions);

            return new PermissionParseResult(permissionDocument, diagnostics);
        }
    }

    private static int ReadRequiredVersion(
        JsonElement root,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        if (!root.TryGetProperty("version", out var versionElement))
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP002",
                PermissionDiagnosticSeverity.Error,
                "Required root property 'version' is missing.",
                path,
                jsonPath: "$.version"));

            return 0;
        }

        if (!versionElement.TryGetInt32(out var version) || version != 1)
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP010",
                PermissionDiagnosticSeverity.Error,
                "Unsupported permission schema version. Only version 1 is supported.",
                path,
                jsonPath: "$.version"));
        }

        return version;
    }

    private static IReadOnlyList<PermissionGroupSpec> ReadGroups(
        JsonElement root,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        if (!root.TryGetProperty("groups", out var groupsElement) || groupsElement.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<PermissionGroupSpec>();
        }

        if (groupsElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP002",
                PermissionDiagnosticSeverity.Error,
                "Root property 'groups' must be an array when provided.",
                path,
                jsonPath: "$.groups"));

            return Array.Empty<PermissionGroupSpec>();
        }

        var groups = new List<PermissionGroupSpec>();
        var index = 0;

        foreach (var groupElement in groupsElement.EnumerateArray())
        {
            var itemPath = $"$.groups[{index}]";
            index++;

            if (groupElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP003",
                    PermissionDiagnosticSeverity.Error,
                    "Permission group entries must be JSON objects.",
                    path,
                    jsonPath: itemPath));
                continue;
            }

            var code = ReadOptionalString(groupElement, "code", itemPath + ".code", diagnostics, path);
            if (string.IsNullOrWhiteSpace(code))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP002",
                    PermissionDiagnosticSeverity.Error,
                    "Permission group property 'code' is required.",
                    path,
                    jsonPath: itemPath + ".code"));
                continue;
            }

            if (!PermissionIdentifier.IsValidGroupCode(code))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP003",
                    PermissionDiagnosticSeverity.Error,
                    $"Permission group code '{code}' is invalid.",
                    path,
                    jsonPath: itemPath + ".code"));
            }

            groups.Add(new PermissionGroupSpec(
                code,
                ReadOptionalString(groupElement, "name", itemPath + ".name", diagnostics, path),
                ReadOptionalString(groupElement, "labelKey", itemPath + ".labelKey", diagnostics, path),
                ReadOptionalString(groupElement, "description", itemPath + ".description", diagnostics, path),
                ReadOptionalString(groupElement, "descriptionKey", itemPath + ".descriptionKey", diagnostics, path),
                ReadOptionalInt(groupElement, "order", itemPath + ".order", diagnostics, path)));
        }

        return groups;
    }

    private static IReadOnlyList<PermissionSpec> ReadPermissions(
        JsonElement root,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        if (!root.TryGetProperty("permissions", out var permissionsElement))
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP002",
                PermissionDiagnosticSeverity.Error,
                "Required root property 'permissions' is missing.",
                path,
                jsonPath: "$.permissions"));

            return Array.Empty<PermissionSpec>();
        }

        if (permissionsElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP002",
                PermissionDiagnosticSeverity.Error,
                "Root property 'permissions' must be an array.",
                path,
                jsonPath: "$.permissions"));

            return Array.Empty<PermissionSpec>();
        }

        var permissions = new List<PermissionSpec>();
        var index = 0;

        foreach (var permissionElement in permissionsElement.EnumerateArray())
        {
            var itemPath = $"$.permissions[{index}]";
            index++;

            if (permissionElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP003",
                    PermissionDiagnosticSeverity.Error,
                    "Permission entries must be JSON objects.",
                    path,
                    jsonPath: itemPath));
                continue;
            }

            var code = ReadOptionalString(permissionElement, "code", itemPath + ".code", diagnostics, path);
            if (string.IsNullOrWhiteSpace(code))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP003",
                    PermissionDiagnosticSeverity.Error,
                    "Permission property 'code' is required.",
                    path,
                    jsonPath: itemPath + ".code"));
                continue;
            }

            if (!PermissionIdentifier.IsValidPermissionCode(code))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP003",
                    PermissionDiagnosticSeverity.Error,
                    $"Permission code '{code}' is invalid. Expected format is 'area.action'.",
                    path,
                    jsonPath: itemPath + ".code"));
            }

            permissions.Add(new PermissionSpec(
                code,
                ReadOptionalString(permissionElement, "name", itemPath + ".name", diagnostics, path),
                ReadOptionalString(permissionElement, "group", itemPath + ".group", diagnostics, path),
                ReadOptionalStringArray(permissionElement, "requires", itemPath + ".requires", "SP011", diagnostics, path),
                ReadOptionalString(permissionElement, "description", itemPath + ".description", diagnostics, path),
                ReadOptionalString(permissionElement, "labelKey", itemPath + ".labelKey", diagnostics, path),
                ReadOptionalString(permissionElement, "descriptionKey", itemPath + ".descriptionKey", diagnostics, path),
                ReadOptionalStringArray(permissionElement, "tags", itemPath + ".tags", "SP003", diagnostics, path),
                ReadOptionalInt(permissionElement, "order", itemPath + ".order", diagnostics, path)));
        }

        return permissions;
    }

    private static void ValidateGroups(
        IReadOnlyList<PermissionGroupSpec> groups,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (!seen.Add(group.Code))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP004",
                    PermissionDiagnosticSeverity.Error,
                    $"Duplicate permission group code '{group.Code}'.",
                    path));
            }
        }
    }

    private static void ValidatePermissions(
        IReadOnlyList<PermissionGroupSpec> groups,
        IReadOnlyList<PermissionSpec> permissions,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        var groupCodes = new HashSet<string>(groups.Select(static group => group.Code), StringComparer.Ordinal);
        var permissionCodes = new Dictionary<string, PermissionSpec>(StringComparer.Ordinal);
        var identifierPaths = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            if (!permissionCodes.ContainsKey(permission.Code))
            {
                permissionCodes.Add(permission.Code, permission);
            }
            else
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP004",
                    PermissionDiagnosticSeverity.Error,
                    $"Duplicate permission code '{permission.Code}'.",
                    path));
            }

            var identifierPath = string.Join(".", PermissionIdentifier.ToIdentifierPath(permission.Code));
            if (identifierPaths.TryGetValue(identifierPath, out var existingCode) &&
                !string.Equals(existingCode, permission.Code, StringComparison.Ordinal))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP005",
                    PermissionDiagnosticSeverity.Error,
                    $"Permission code '{permission.Code}' maps to generated identifier '{identifierPath}', which collides with '{existingCode}'.",
                    path));
            }
            else if (HasIdentifierPrefixCollision(identifierPaths, identifierPath, out existingCode))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP005",
                    PermissionDiagnosticSeverity.Error,
                    $"Permission code '{permission.Code}' maps to generated identifier '{identifierPath}', which conflicts with '{existingCode}' because one identifier path is a prefix of the other.",
                    path));
            }
            else
            {
                identifierPaths[identifierPath] = permission.Code;
            }

            if (!string.IsNullOrWhiteSpace(permission.Group) && !groupCodes.Contains(permission.Group!))
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    "SP006",
                    PermissionDiagnosticSeverity.Warning,
                    $"Permission '{permission.Code}' references unknown group '{permission.Group}'.",
                    path));
            }
        }

        foreach (var permission in permissions)
        {
            foreach (var requiredCode in permission.Requires)
            {
                if (!permissionCodes.ContainsKey(requiredCode))
                {
                    diagnostics.Add(new PermissionValidationDiagnostic(
                        "SP011",
                        PermissionDiagnosticSeverity.Error,
                        $"Permission '{permission.Code}' requires unknown permission '{requiredCode}'.",
                        path));
                }
            }
        }

        ValidateCycles(permissionCodes, diagnostics, path);
    }

    private static bool HasIdentifierPrefixCollision(
        IReadOnlyDictionary<string, string> identifierPaths,
        string identifierPath,
        out string existingCode)
    {
        foreach (var item in identifierPaths)
        {
            if (identifierPath.StartsWith(item.Key + ".", StringComparison.Ordinal) ||
                item.Key.StartsWith(identifierPath + ".", StringComparison.Ordinal))
            {
                existingCode = item.Value;
                return true;
            }
        }

        existingCode = string.Empty;
        return false;
    }

    private static void ValidateCycles(
        IReadOnlyDictionary<string, PermissionSpec> permissions,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        var states = new Dictionary<string, VisitState>(StringComparer.Ordinal);
        var stack = new List<string>();
        var reportedCycles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var code in permissions.Keys.OrderBy(static code => code, StringComparer.Ordinal))
        {
            Visit(code, permissions, states, stack, reportedCycles, diagnostics, path);
        }
    }

    private static void Visit(
        string code,
        IReadOnlyDictionary<string, PermissionSpec> permissions,
        Dictionary<string, VisitState> states,
        List<string> stack,
        HashSet<string> reportedCycles,
        List<PermissionValidationDiagnostic> diagnostics,
        string? path)
    {
        if (!permissions.ContainsKey(code))
        {
            return;
        }

        if (states.TryGetValue(code, out var state))
        {
            if (state == VisitState.Visiting)
            {
                var start = stack.IndexOf(code);
                var cycle = start >= 0 ? stack.Skip(start).Concat(new[] { code }).ToArray() : new[] { code, code };
                var cycleText = string.Join(" -> ", cycle);

                if (reportedCycles.Add(cycleText))
                {
                    diagnostics.Add(new PermissionValidationDiagnostic(
                        "SP012",
                        PermissionDiagnosticSeverity.Error,
                        $"Permission requires contains a circular dependency: {cycleText}.",
                        path));
                }
            }

            return;
        }

        states[code] = VisitState.Visiting;
        stack.Add(code);

        foreach (var requiredCode in permissions[code].Requires)
        {
            Visit(requiredCode, permissions, states, stack, reportedCycles, diagnostics, path);
        }

        stack.RemoveAt(stack.Count - 1);
        states[code] = VisitState.Visited;
    }

    private static string? ReadOptionalString(
        JsonElement element,
        string propertyName,
        string propertyPath,
        List<PermissionValidationDiagnostic> diagnostics,
        string? filePath)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP003",
                PermissionDiagnosticSeverity.Error,
                $"Property '{propertyPath}' must be a string.",
                filePath,
                jsonPath: propertyPath));
            return null;
        }

        return property.GetString();
    }

    private static IReadOnlyList<string> ReadOptionalStringArray(
        JsonElement element,
        string propertyName,
        string propertyPath,
        string diagnosticId,
        List<PermissionValidationDiagnostic> diagnostics,
        string? filePath)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<string>();
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                diagnosticId,
                PermissionDiagnosticSeverity.Error,
                $"Property '{propertyPath}' must be an array of strings.",
                filePath,
                jsonPath: propertyPath));
            return Array.Empty<string>();
        }

        var values = new List<string>();
        var index = 0;
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                diagnostics.Add(new PermissionValidationDiagnostic(
                    diagnosticId,
                    PermissionDiagnosticSeverity.Error,
                    $"Property '{propertyPath}[{index}]' must be a string.",
                    filePath,
                    jsonPath: propertyPath + "[" + index + "]"));
            }
            else
            {
                values.Add(item.GetString() ?? string.Empty);
            }

            index++;
        }

        return values;
    }

    private static int? ReadOptionalInt(
        JsonElement element,
        string propertyName,
        string propertyPath,
        List<PermissionValidationDiagnostic> diagnostics,
        string? filePath)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (!property.TryGetInt32(out var value))
        {
            diagnostics.Add(new PermissionValidationDiagnostic(
                "SP003",
                PermissionDiagnosticSeverity.Error,
                $"Property '{propertyPath}' must be an integer.",
                filePath,
                jsonPath: propertyPath));
            return null;
        }

        return value;
    }

    private static int? ToOneBasedInt(long? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return checked((int)value.Value + 1);
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
