using System.Collections.Generic;

namespace Senlinz.Permissions.Generation;

public sealed class PermissionSpec
{
    public PermissionSpec(
        string code,
        string? name,
        string? group,
        IReadOnlyList<string> requires,
        string? description,
        string? labelKey,
        string? descriptionKey,
        IReadOnlyList<string> tags,
        int? order)
    {
        Code = code;
        Name = name;
        Group = group;
        Requires = requires;
        Description = description;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
        Tags = tags;
        Order = order;
    }

    public string Code { get; }

    public string? Name { get; }

    public string? Group { get; }

    public IReadOnlyList<string> Requires { get; }

    public string? Description { get; }

    public string? LabelKey { get; }

    public string? DescriptionKey { get; }

    public IReadOnlyList<string> Tags { get; }

    public int? Order { get; }
}
