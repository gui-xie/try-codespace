using System.Collections.Generic;

namespace Senlinz.Permissions.Generation;

public sealed class PermissionDocument
{
    public PermissionDocument(
        int version,
        string? generatedNamespace,
        string? className,
        string? catalogClassName,
        IReadOnlyList<PermissionGroupSpec> groups,
        IReadOnlyList<PermissionSpec> permissions)
    {
        Version = version;
        GeneratedNamespace = generatedNamespace;
        ClassName = className;
        CatalogClassName = catalogClassName;
        Groups = groups;
        Permissions = permissions;
    }

    public int Version { get; }

    public string? GeneratedNamespace { get; }

    public string? ClassName { get; }

    public string? CatalogClassName { get; }

    public IReadOnlyList<PermissionGroupSpec> Groups { get; }

    public IReadOnlyList<PermissionSpec> Permissions { get; }
}
