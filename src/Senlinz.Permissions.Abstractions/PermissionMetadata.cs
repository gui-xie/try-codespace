namespace Senlinz.Permissions;

public sealed class PermissionMetadata
{
    public PermissionMetadata(
        string? name = null,
        string? description = null,
        string? labelKey = null,
        string? descriptionKey = null)
    {
        Name = name;
        Description = description;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
    }

    public string? Name { get; }

    public string? Description { get; }

    public string? LabelKey { get; }

    public string? DescriptionKey { get; }
}
