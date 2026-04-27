using System;

namespace Senlinz.Permissions;

public sealed class PermissionGroupDefinition
{
    public PermissionGroupDefinition(
        string code,
        string? name = null,
        string? labelKey = null,
        string? description = null,
        string? descriptionKey = null,
        int? order = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Permission group code cannot be empty.", nameof(code));
        }

        Code = code;
        Name = name;
        LabelKey = labelKey;
        Description = description;
        DescriptionKey = descriptionKey;
        Order = order;
    }

    public string Code { get; }

    public string? Name { get; }

    public string? LabelKey { get; }

    public string? Description { get; }

    public string? DescriptionKey { get; }

    public int? Order { get; }
}
