using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Senlinz.Permissions.Generation;

namespace Senlinz.Permissions;

[Generator]
public sealed class PermissionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => PermissionGeneratorOptions.From(provider));

        var filesProvider = context.AdditionalTextsProvider.Collect();
        var pipeline = filesProvider.Combine(optionsProvider).Combine(context.CompilationProvider);

        context.RegisterSourceOutput(
            pipeline,
            static (sourceProductionContext, value) =>
            {
                var ((files, options), compilation) = value;
                Execute(sourceProductionContext, files, options, compilation);
            });
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<AdditionalText> files,
        PermissionGeneratorOptions options,
        Compilation compilation)
    {
        var permissionFile = FindPermissionFile(files, options.PermissionFile);
        if (permissionFile is null)
        {
            var id = options.Strict ? "SP009" : "SP008";
            var severity = options.Strict ? PermissionDiagnosticSeverity.Error : PermissionDiagnosticSeverity.Warning;
            ReportDiagnostic(
                context,
                new PermissionValidationDiagnostic(
                    id,
                    severity,
                    $"Permission file '{options.PermissionFile}' was not found."));
            return;
        }

        var sourceText = permissionFile.GetText(context.CancellationToken);
        if (sourceText is null)
        {
            ReportDiagnostic(
                context,
                new PermissionValidationDiagnostic(
                    "SP001",
                    PermissionDiagnosticSeverity.Error,
                    $"Permission file '{permissionFile.Path}' could not be read.",
                    permissionFile.Path));
            return;
        }

        var parseResult = PermissionJsonParser.Parse(sourceText.ToString(), permissionFile.Path);
        foreach (var diagnostic in parseResult.Diagnostics)
        {
            ReportDiagnostic(context, diagnostic, permissionFile.Path);
        }

        if (parseResult.HasErrors || parseResult.Document is null)
        {
            return;
        }

        var document = parseResult.Document;
        var generatedNamespace = ResolveNamespace(options, document);
        var permissionClassName = ResolveClassName(options.PermissionClassName, document.ClassName, "Permissions");
        var catalogClassName = ResolveClassName(options.CatalogClassName, document.CatalogClassName, "PermissionCatalog");

        if (!PermissionIdentifier.IsValidQualifiedName(generatedNamespace))
        {
            ReportInvalidName(context, $"Generated namespace '{generatedNamespace}' is not a valid C# namespace.", permissionFile.Path);
            return;
        }

        if (!PermissionIdentifier.IsValidIdentifier(permissionClassName))
        {
            ReportInvalidName(context, $"Generated permission class name '{permissionClassName}' is not a valid C# identifier.", permissionFile.Path);
            return;
        }

        if (!PermissionIdentifier.IsValidIdentifier(catalogClassName))
        {
            ReportInvalidName(context, $"Generated catalog class name '{catalogClassName}' is not a valid C# identifier.", permissionFile.Path);
            return;
        }

        if (string.Equals(permissionClassName, catalogClassName, StringComparison.Ordinal))
        {
            ReportInvalidName(context, "Generated permission class name and catalog class name must be different.", permissionFile.Path);
            return;
        }

        context.AddSource(
            permissionClassName + ".g.cs",
            SourceText.From(PermissionSourceEmitter.EmitPermissions(document, generatedNamespace, permissionClassName), Encoding.UTF8));

        context.AddSource(
            catalogClassName + ".g.cs",
            SourceText.From(PermissionSourceEmitter.EmitCatalog(document, generatedNamespace, catalogClassName), Encoding.UTF8));

        if (options.GenerateLString && HasLocalizationReference(compilation))
        {
            context.AddSource(
                "PermissionL.g.cs",
                SourceText.From(PermissionSourceEmitter.EmitLString(document, generatedNamespace), Encoding.UTF8));
        }
    }

    private static AdditionalText? FindPermissionFile(ImmutableArray<AdditionalText> files, string configuredFile)
    {
        foreach (var file in files.OrderBy(static file => file.Path, StringComparer.Ordinal))
        {
            if (MatchesConfiguredFile(file.Path, configuredFile))
            {
                return file;
            }
        }

        return null;
    }

    private static bool MatchesConfiguredFile(string filePath, string configuredFile)
    {
        var normalizedFilePath = NormalizePath(filePath);
        var normalizedConfiguredFile = NormalizePath(configuredFile).TrimStart('.', '/');

        if (Path.IsPathRooted(configuredFile))
        {
            return string.Equals(normalizedFilePath, NormalizePath(configuredFile), StringComparison.OrdinalIgnoreCase);
        }

        if (!normalizedConfiguredFile.Contains("/"))
        {
            return string.Equals(Path.GetFileName(normalizedFilePath), normalizedConfiguredFile, StringComparison.OrdinalIgnoreCase);
        }

        return normalizedFilePath.EndsWith("/" + normalizedConfiguredFile, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string ResolveNamespace(PermissionGeneratorOptions options, PermissionDocument document)
    {
        if (!string.IsNullOrWhiteSpace(options.GeneratedNamespace))
        {
            return options.GeneratedNamespace!;
        }

        if (!string.IsNullOrWhiteSpace(document.GeneratedNamespace))
        {
            return document.GeneratedNamespace!;
        }

        if (!string.IsNullOrWhiteSpace(options.RootNamespace))
        {
            return options.RootNamespace!;
        }

        return PermissionIdentifier.SanitizeQualifiedName(options.AssemblyName ?? "Senlinz.Permissions");
    }

    private static string ResolveClassName(string propertyValue, string? documentValue, string defaultValue)
    {
        if (!string.Equals(propertyValue, defaultValue, StringComparison.Ordinal))
        {
            return propertyValue;
        }

        return string.IsNullOrWhiteSpace(documentValue) ? defaultValue : documentValue!;
    }

    private static bool HasLocalizationReference(Compilation compilation)
    {
        return compilation.ReferencedAssemblyNames.Any(static assembly =>
            string.Equals(assembly.Name, "Senlinz.Localization.Abstractions", StringComparison.Ordinal));
    }

    private static void ReportInvalidName(SourceProductionContext context, string message, string path)
    {
        ReportDiagnostic(
            context,
            new PermissionValidationDiagnostic("SP007", PermissionDiagnosticSeverity.Error, message, path));
    }

    private static void ReportDiagnostic(
        SourceProductionContext context,
        PermissionValidationDiagnostic diagnostic,
        string? fallbackPath = null)
    {
        var message = string.IsNullOrEmpty(diagnostic.JsonPath)
            ? diagnostic.Message
            : diagnostic.Message + " (" + diagnostic.JsonPath + ")";

        var descriptor = PermissionGeneratorDiagnosticDescriptors.Get(diagnostic.Id, message);
        var location = CreateLocation(diagnostic, fallbackPath);
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
    }

    private static Location CreateLocation(PermissionValidationDiagnostic diagnostic, string? fallbackPath)
    {
        var filePath = diagnostic.FilePath ?? fallbackPath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Location.None;
        }

        if (diagnostic.Line.HasValue && diagnostic.Column.HasValue)
        {
            var start = new LinePosition(Math.Max(0, diagnostic.Line.Value - 1), Math.Max(0, diagnostic.Column.Value - 1));
            return Location.Create(
                filePath!,
                new TextSpan(0, 0),
                new LinePositionSpan(start, start));
        }

        var zero = new LinePosition(0, 0);
        return Location.Create(filePath!, new TextSpan(0, 0), new LinePositionSpan(zero, zero));
    }
}
