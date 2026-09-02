namespace TablerAuth.Application.Admin;

public sealed class RoleListItem
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public int UserCount { get; init; }

    public IReadOnlyList<string> Permissions { get; init; } = [];
}
