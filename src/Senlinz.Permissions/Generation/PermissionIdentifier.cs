using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace Senlinz.Permissions.Generation;

public static class PermissionIdentifier
{
    private static readonly Regex PermissionCodePattern = new Regex(
        "^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$",
        RegexOptions.CultureInvariant);

    private static readonly Regex GroupCodePattern = new Regex(
        "^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$",
        RegexOptions.CultureInvariant);

    public static bool IsValidPermissionCode(string code)
    {
        return PermissionCodePattern.IsMatch(code);
    }

    public static bool IsValidGroupCode(string code)
    {
        return GroupCodePattern.IsMatch(code);
    }

    public static IReadOnlyList<string> ToIdentifierPath(string code)
    {
        return code.Split('.').Select(ToPascalIdentifier).ToArray();
    }

    public static string ToFlatIdentifier(string code)
    {
        var builder = new StringBuilder();
        foreach (var part in ToIdentifierPath(code))
        {
            builder.Append(part);
        }

        return builder.ToString();
    }

    public static string ToPascalIdentifier(string segment)
    {
        if (string.IsNullOrEmpty(segment))
        {
            return "_";
        }

        var builder = new StringBuilder(segment.Length);
        var capitalizeNext = true;

        foreach (var character in segment)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                if (builder.Length == 0 && char.IsDigit(character))
                {
                    builder.Append('_');
                }

                builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        if (builder.Length == 0)
        {
            return "_";
        }

        var identifier = builder.ToString();
        return SyntaxFacts.IsValidIdentifier(identifier) ? identifier : "_" + identifier;
    }

    public static bool IsValidIdentifier(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && SyntaxFacts.IsValidIdentifier(name);
    }

    public static bool IsValidQualifiedName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        foreach (var part in name.Split('.'))
        {
            if (!IsValidIdentifier(part))
            {
                return false;
            }
        }

        return true;
    }

    public static string SanitizeQualifiedName(string value)
    {
        var parts = new List<string>();
        var current = new StringBuilder();

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                current.Append(character);
            }
            else
            {
                AddSanitizedPart(parts, current);
            }
        }

        AddSanitizedPart(parts, current);
        return parts.Count == 0 ? "Senlinz.Permissions" : string.Join(".", parts);
    }

    private static void AddSanitizedPart(List<string> parts, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        parts.Add(ToPascalIdentifier(current.ToString()));
        current.Clear();
    }
}
