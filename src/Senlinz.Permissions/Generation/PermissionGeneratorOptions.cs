using Microsoft.CodeAnalysis.Diagnostics;

namespace Senlinz.Permissions.Generation;

internal sealed class PermissionGeneratorOptions
{
    private PermissionGeneratorOptions(
        string permissionFile,
        string? generatedNamespace,
        string permissionClassName,
        string catalogClassName,
        bool strict,
        bool generateLString,
        string? rootNamespace,
        string? assemblyName)
    {
        PermissionFile = permissionFile;
        GeneratedNamespace = generatedNamespace;
        PermissionClassName = permissionClassName;
        CatalogClassName = catalogClassName;
        Strict = strict;
        GenerateLString = generateLString;
        RootNamespace = rootNamespace;
        AssemblyName = assemblyName;
    }

    public string PermissionFile { get; }

    public string? GeneratedNamespace { get; }

    public string PermissionClassName { get; }

    public string CatalogClassName { get; }

    public bool Strict { get; }

    public bool GenerateLString { get; }

    public string? RootNamespace { get; }

    public string? AssemblyName { get; }

    public static PermissionGeneratorOptions From(AnalyzerConfigOptionsProvider provider)
    {
        var globalOptions = provider.GlobalOptions;

        return new PermissionGeneratorOptions(
            GetString(globalOptions, "SenlinzPermissionFile", "permission.json"),
            GetOptionalString(globalOptions, "SenlinzPermissionNamespace"),
            GetString(globalOptions, "SenlinzPermissionClassName", "Permissions"),
            GetString(globalOptions, "SenlinzPermissionCatalogClassName", "PermissionCatalog"),
            GetBool(globalOptions, "SenlinzPermissionStrict", defaultValue: true),
            GetBool(globalOptions, "SenlinzPermissionGenerateLString", defaultValue: false),
            GetOptionalString(globalOptions, "RootNamespace"),
            GetOptionalString(globalOptions, "AssemblyName"));
    }

    private static string GetString(AnalyzerConfigOptions options, string name, string defaultValue)
    {
        return GetOptionalString(options, name) ?? defaultValue;
    }

    private static string? GetOptionalString(AnalyzerConfigOptions options, string name)
    {
        return options.TryGetValue("build_property." + name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value!.Trim()
            : null;
    }

    private static bool GetBool(AnalyzerConfigOptions options, string name, bool defaultValue)
    {
        if (!options.TryGetValue("build_property." + name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
