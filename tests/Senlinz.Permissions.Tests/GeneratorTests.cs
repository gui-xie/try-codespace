using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Senlinz.Permissions;
using Senlinz.Permissions.Generation;
using Xunit;

namespace Senlinz.Permissions.Tests;

public sealed class GeneratorTests
{
    [Fact]
    public void Generates_constants_and_catalog()
    {
        var result = RunGenerator(
            """
            {
              "version": 1,
              "groups": [
                { "code": "users", "name": "Users" }
              ],
              "permissions": [
                { "code": "users.read", "name": "View users", "group": "users" },
                { "code": "users.create", "requires": ["users.read"] },
                { "code": "system.audit.view" }
              ]
            }
            """);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        var permissionsSource = result.GeneratedSources.Single(source => source.HintName == "Permissions.g.cs").SourceText.ToString();
        Assert.Contains("public static class Users", permissionsSource);
        Assert.Contains("public const string Read = \"users.read\";", permissionsSource);
        Assert.Contains("public static class System", permissionsSource);
        Assert.Contains("public static class Audit", permissionsSource);
        Assert.Contains("public const string View = \"system.audit.view\";", permissionsSource);

        var catalogSource = result.GeneratedSources.Single(source => source.HintName == "PermissionCatalog.g.cs").SourceText.ToString();
        Assert.Contains("new global::Senlinz.Permissions.PermissionDefinition(", catalogSource);
        Assert.Contains("requires: new[] { \"users.read\" }", catalogSource);
        Assert.Contains("public static global::Senlinz.Permissions.PermissionCatalog CreateCatalog()", catalogSource);
    }

    [Fact]
    public void Uses_namespace_and_class_overrides()
    {
        var result = RunGenerator(
            """
            {
              "version": 1,
              "namespace": "Json.Security",
              "className": "AppPermissions",
              "catalogClassName": "AppPermissionCatalog",
              "permissions": [
                { "code": "users.read" }
              ]
            }
            """,
            new Dictionary<string, string>
            {
                ["build_property.SenlinzPermissionNamespace"] = "Configured.Security"
            });

        var permissionsSource = result.GeneratedSources.Single(source => source.HintName == "AppPermissions.g.cs").SourceText.ToString();
        Assert.Contains("namespace Configured.Security", permissionsSource);
    }

    [Fact]
    public void Reports_missing_file_when_strict()
    {
        var result = RunGenerator(json: null);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP009");
    }

    [Fact]
    public void Warns_missing_file_when_not_strict()
    {
        var result = RunGenerator(
            json: null,
            new Dictionary<string, string>
            {
                ["build_property.SenlinzPermissionStrict"] = "false"
            });

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP008");
    }

    private static GeneratorDriverRunResult RunGenerator(
        string? json,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        var parseOptions = (CSharpParseOptions)CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "SampleApp",
            new[] { CSharpSyntaxTree.ParseText("namespace SampleApp; public sealed class Startup { }", parseOptions) },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additionalTexts = json is null
            ? Array.Empty<AdditionalText>()
            : new AdditionalText[] { new InMemoryAdditionalText("/src/SampleApp/permission.json", json) };

        var options = new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "SampleApp",
            ["build_property.AssemblyName"] = "SampleApp"
        };

        if (properties is not null)
        {
            foreach (var item in properties)
            {
                options[item.Key] = item.Value;
            }
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new PermissionSourceGenerator().AsSourceGenerator() },
            additionalTexts,
            parseOptions,
            new InMemoryAnalyzerConfigOptionsProvider(options));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();

        var outputDiagnostics = outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Concat(diagnostics);

        return new GeneratorDriverRunResult(
            runResult.Results.SelectMany(result => result.GeneratedSources).ToArray(),
            runResult.Diagnostics.Concat(outputDiagnostics).ToArray());
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedPlatformAssemblies is null)
        {
            yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(PermissionDefinition).Assembly.Location);
            yield break;
        }

        foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
        {
            yield return MetadataReference.CreateFromFile(path);
        }

        yield return MetadataReference.CreateFromFile(typeof(PermissionDefinition).Assembly.Location);
    }

    private sealed class GeneratorDriverRunResult
    {
        public GeneratorDriverRunResult(
            IReadOnlyList<GeneratedSourceResult> generatedSources,
            IReadOnlyList<Diagnostic> diagnostics)
        {
            GeneratedSources = generatedSources;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<GeneratedSourceResult> GeneratedSources { get; }

        public IReadOnlyList<Diagnostic> Diagnostics { get; }
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText sourceText;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            sourceText = SourceText.From(text);
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return sourceText;
        }
    }

    private sealed class InMemoryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions options;

        public InMemoryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> values)
        {
            options = new InMemoryAnalyzerConfigOptions(values);
        }

        public override AnalyzerConfigOptions GlobalOptions => options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return options;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return options;
        }
    }

    private sealed class InMemoryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> values;

        public InMemoryAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            this.values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return values.TryGetValue(key, out value!);
        }
    }
}
