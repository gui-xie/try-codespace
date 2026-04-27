namespace Senlinz.Permissions.Generation;

public sealed class PermissionGroupSpec
{
    public PermissionGroupSpec(
        string code,
        string? name,
        string? labelKey,
        string? description,
        string? descriptionKey,
        int? order)
    {
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
