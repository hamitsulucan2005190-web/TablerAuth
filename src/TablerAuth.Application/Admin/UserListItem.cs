using TablerAuth.Domain.Identity;

namespace TablerAuth.Application.Admin;

public sealed class UserListItem
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsAdmin => Roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);
}
