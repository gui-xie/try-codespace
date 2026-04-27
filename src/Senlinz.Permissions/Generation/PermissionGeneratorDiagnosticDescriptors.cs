using Microsoft.CodeAnalysis;

namespace Senlinz.Permissions.Generation;

internal static class PermissionGeneratorDiagnosticDescriptors
{
    private const string Category = "Senlinz.Permissions";

    public static DiagnosticDescriptor Get(string id, string message)
    {
        return id switch
        {
            "SP001" => Error(id, "Invalid permission JSON", message),
            "SP002" => Error(id, "Missing permission property", message),
            "SP003" => Error(id, "Invalid permission code", message),
            "SP004" => Error(id, "Duplicate permission code", message),
            "SP005" => Error(id, "Generated identifier collision", message),
            "SP006" => Warning(id, "Unknown permission group", message),
            "SP007" => Error(id, "Invalid generated C# name", message),
            "SP008" => Warning(id, "Permission file missing", message),
            "SP009" => Error(id, "Permission file missing", message),
            "SP010" => Error(id, "Unsupported permission schema version", message),
            "SP011" => Error(id, "Invalid permission dependency", message),
            "SP012" => Error(id, "Circular permission dependency", message),
            _ => Error(id, "Permission generator diagnostic", message)
        };
    }

    private static DiagnosticDescriptor Error(string id, string title, string message)
    {
        return new DiagnosticDescriptor(id, title, message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
    }

    private static DiagnosticDescriptor Warning(string id, string title, string message)
    {
        return new DiagnosticDescriptor(id, title, message, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
    }
}
