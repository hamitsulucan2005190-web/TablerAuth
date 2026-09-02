namespace TablerAuth.Application.Admin;

public interface IAdminDirectoryService
{
    Task<IReadOnlyList<UserListItem>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleListItem>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<AdminActionResult> SetAdminRoleAsync(
        string userId,
        bool isAdmin,
        string actingUserId,
        CancellationToken cancellationToken = default);
}
