namespace Senlinz.Permissions.Generation;

public sealed class PermissionValidationDiagnostic
{
    public PermissionValidationDiagnostic(
        string id,
        PermissionDiagnosticSeverity severity,
        string message,
        string? filePath = null,
        string? jsonPath = null,
        int? line = null,
        int? column = null)
    {
        Id = id;
        Severity = severity;
        Message = message;
        FilePath = filePath;
        JsonPath = jsonPath;
        Line = line;
        Column = column;
    }

    public string Id { get; }

    public PermissionDiagnosticSeverity Severity { get; }

    public string Message { get; }

    public string? FilePath { get; }

    public string? JsonPath { get; }

    public int? Line { get; }

    public int? Column { get; }
}
