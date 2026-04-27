using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Senlinz.Permissions.Generation;

internal static class PermissionSourceEmitter
{
    private const string GeneratedCodeAttribute =
        "[global::System.CodeDom.Compiler.GeneratedCode(\"Senlinz.Permissions\", \"1.0.0.0\")]";

    public static string EmitPermissions(PermissionDocument document, string generatedNamespace, string className)
    {
        var root = BuildConstantTree(document.Permissions);
        var builder = CreateHeader(generatedNamespace);

        AppendLine(builder, 1, GeneratedCodeAttribute);
        AppendLine(builder, 1, $"public static partial class {className}");
        AppendLine(builder, 1, "{");
        AppendConstants(builder, root, 2);
        AppendLine(builder, 1, "}");
        builder.AppendLine("}");

        return builder.ToString();
    }

    public static string EmitCatalog(PermissionDocument document, string generatedNamespace, string catalogClassName)
    {
        var builder = CreateHeader(generatedNamespace);

        AppendLine(builder, 1, GeneratedCodeAttribute);
        AppendLine(builder, 1, $"public static partial class {catalogClassName}");
        AppendLine(builder, 1, "{");
        AppendLine(builder, 2, "public static global::System.Collections.Generic.IReadOnlyList<global::Senlinz.Permissions.PermissionGroupDefinition> Groups { get; } =");
        AppendLine(builder, 3, "new global::Senlinz.Permissions.PermissionGroupDefinition[]");
        AppendLine(builder, 3, "{");

        for (var i = 0; i < document.Groups.Count; i++)
        {
            AppendGroup(builder, document.Groups[i], 4);
            builder.AppendLine(i == document.Groups.Count - 1 ? string.Empty : ",");
        }

        AppendLine(builder, 3, "};");
        builder.AppendLine();
        AppendLine(builder, 2, "public static global::System.Collections.Generic.IReadOnlyList<global::Senlinz.Permissions.PermissionDefinition> All { get; } =");
        AppendLine(builder, 3, "new global::Senlinz.Permissions.PermissionDefinition[]");
        AppendLine(builder, 3, "{");

        for (var i = 0; i < document.Permissions.Count; i++)
        {
            AppendPermission(builder, document.Permissions[i], 4);
            builder.AppendLine(i == document.Permissions.Count - 1 ? string.Empty : ",");
        }

        AppendLine(builder, 3, "};");
        builder.AppendLine();
        AppendLine(builder, 2, "public static global::Senlinz.Permissions.PermissionCatalog CreateCatalog()");
        AppendLine(builder, 2, "{");
        AppendLine(builder, 3, "return new global::Senlinz.Permissions.PermissionCatalog(All, Groups);");
        AppendLine(builder, 2, "}");
        AppendLine(builder, 1, "}");
        builder.AppendLine("}");

        return builder.ToString();
    }

    public static string EmitLString(PermissionDocument document, string generatedNamespace)
    {
        var builder = CreateHeader(generatedNamespace);

        AppendLine(builder, 1, GeneratedCodeAttribute);
        AppendLine(builder, 1, "public static partial class PermissionL");
        AppendLine(builder, 1, "{");

        foreach (var permission in document.Permissions)
        {
            var name = PermissionIdentifier.ToFlatIdentifier(permission.Code);
            var key = permission.LabelKey ?? "permissions." + permission.Code + ".label";
            var fallback = permission.Name ?? permission.Code;
            AppendLine(
                builder,
                2,
                $"public static readonly global::Senlinz.Localization.LString {name} = new global::Senlinz.Localization.LString({Literal(key)}, {Literal(fallback)});");
        }

        AppendLine(builder, 1, "}");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static ConstantNode BuildConstantTree(IEnumerable<PermissionSpec> permissions)
    {
        var root = new ConstantNode();

        foreach (var permission in permissions)
        {
            var identifiers = PermissionIdentifier.ToIdentifierPath(permission.Code);
            var node = root;

            for (var i = 0; i < identifiers.Count - 1; i++)
            {
                if (!node.Classes.TryGetValue(identifiers[i], out var child))
                {
                    child = new ConstantNode();
                    node.Classes.Add(identifiers[i], child);
                }

                node = child;
            }

            node.Constants[identifiers[identifiers.Count - 1]] = permission.Code;
        }

        return root;
    }

    private static void AppendConstants(StringBuilder builder, ConstantNode node, int indent)
    {
        var wroteMember = false;

        foreach (var constant in node.Constants)
        {
            AppendLine(builder, indent, $"public const string {constant.Key} = {Literal(constant.Value)};");
            wroteMember = true;
        }

        foreach (var child in node.Classes)
        {
            if (wroteMember)
            {
                builder.AppendLine();
            }

            AppendLine(builder, indent, $"public static class {child.Key}");
            AppendLine(builder, indent, "{");
            AppendConstants(builder, child.Value, indent + 1);
            AppendLine(builder, indent, "}");
            wroteMember = true;
        }
    }

    private static void AppendGroup(StringBuilder builder, PermissionGroupSpec group, int indent)
    {
        var arguments = new List<string> { Literal(group.Code) };
        AddOptional(arguments, "name", group.Name);
        AddOptional(arguments, "labelKey", group.LabelKey);
        AddOptional(arguments, "description", group.Description);
        AddOptional(arguments, "descriptionKey", group.DescriptionKey);
        AddOptional(arguments, "order", group.Order);

        AppendCreation(builder, "global::Senlinz.Permissions.PermissionGroupDefinition", arguments, indent);
    }

    private static void AppendPermission(StringBuilder builder, PermissionSpec permission, int indent)
    {
        var arguments = new List<string> { Literal(permission.Code) };
        AddOptional(arguments, "name", permission.Name);
        AddOptional(arguments, "group", permission.Group);
        AddOptional(arguments, "requires", permission.Requires);
        AddOptional(arguments, "description", permission.Description);
        AddOptional(arguments, "labelKey", permission.LabelKey);
        AddOptional(arguments, "descriptionKey", permission.DescriptionKey);
        AddOptional(arguments, "tags", permission.Tags);
        AddOptional(arguments, "order", permission.Order);

        AppendCreation(builder, "global::Senlinz.Permissions.PermissionDefinition", arguments, indent);
    }

    private static void AppendCreation(StringBuilder builder, string typeName, IReadOnlyList<string> arguments, int indent)
    {
        if (arguments.Count == 1)
        {
            AppendLine(builder, indent, $"new {typeName}({arguments[0]})");
            return;
        }

        AppendLine(builder, indent, $"new {typeName}(");
        for (var i = 0; i < arguments.Count; i++)
        {
            var suffix = i == arguments.Count - 1 ? string.Empty : ",";
            AppendLine(builder, indent + 1, arguments[i] + suffix);
        }

        AppendLine(builder, indent, ")");
    }

    private static void AddOptional(List<string> arguments, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            arguments.Add(name + ": " + Literal(value));
        }
    }

    private static void AddOptional(List<string> arguments, string name, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            arguments.Add(name + ": new[] { " + string.Join(", ", values.Select(Literal)) + " }");
        }
    }

    private static void AddOptional(List<string> arguments, string name, int? value)
    {
        if (value.HasValue)
        {
            arguments.Add(name + ": " + value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static StringBuilder CreateHeader(string generatedNamespace)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace " + generatedNamespace);
        builder.AppendLine("{");
        return builder;
    }

    private static void AppendLine(StringBuilder builder, int indent, string text)
    {
        builder.Append(' ', indent * 4);
        builder.AppendLine(text);
    }

    private static string Literal(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    builder.Append(@"\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\0':
                    builder.Append(@"\0");
                    break;
                case '\a':
                    builder.Append(@"\a");
                    break;
                case '\b':
                    builder.Append(@"\b");
                    break;
                case '\f':
                    builder.Append(@"\f");
                    break;
                case '\n':
                    builder.Append(@"\n");
                    break;
                case '\r':
                    builder.Append(@"\r");
                    break;
                case '\t':
                    builder.Append(@"\t");
                    break;
                case '\v':
                    builder.Append(@"\v");
                    break;
                default:
                    if (char.IsControl(character))
                    {
                        builder.Append(@"\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private sealed class ConstantNode
    {
        public SortedDictionary<string, ConstantNode> Classes { get; } = new SortedDictionary<string, ConstantNode>(StringComparer.Ordinal);

        public SortedDictionary<string, string> Constants { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);
    }
}
