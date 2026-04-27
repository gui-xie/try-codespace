using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Senlinz.Permissions;

public sealed class PermissionDefinition
{
    public PermissionDefinition(
        string code,
        string? name = null,
        string? group = null,
        IReadOnlyList<string>? requires = null,
        string? description = null,
        string? labelKey = null,
        string? descriptionKey = null,
        IReadOnlyList<string>? tags = null,
        int? order = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Permission code cannot be empty.", nameof(code));
        }

        Code = code;
        Name = name;
        Group = group;
        Requires = CopyList(requires);
        Description = description;
        LabelKey = labelKey;
        DescriptionKey = descriptionKey;
        Tags = CopyList(tags);
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

    private static IReadOnlyList<string> CopyList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        var copy = new string[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            copy[i] = values[i];
        }

        return new ReadOnlyCollection<string>(copy);
    }
}
