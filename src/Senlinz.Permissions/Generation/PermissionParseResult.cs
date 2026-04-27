using System.Collections.Generic;
using System.Linq;

namespace Senlinz.Permissions.Generation;

public sealed class PermissionParseResult
{
    public PermissionParseResult(
        PermissionDocument? document,
        IReadOnlyList<PermissionValidationDiagnostic> diagnostics)
    {
        Document = document;
        Diagnostics = diagnostics;
    }

    public PermissionDocument? Document { get; }

    public IReadOnlyList<PermissionValidationDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == PermissionDiagnosticSeverity.Error);
}
