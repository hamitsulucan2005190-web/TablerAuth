using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TablerAuth.Application.Admin;
using TablerAuth.Application.Auth;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Infrastructure.Identity;

public class AdminDirectoryService : IAdminDirectoryService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminDirectoryService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    public async Task<IReadOnlyList<UserListItem>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var items = new List<UserListItem>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Roles = roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray()
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<RoleListItem>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var items = new List<RoleListItem>(roles.Count);
        foreach (var role in roles)
        {
            var name = role.Name ?? string.Empty;
            var userCount = string.IsNullOrEmpty(name)
                ? 0
                : (await _userManager.GetUsersInRoleAsync(name)).Count;

            items.Add(new RoleListItem
            {
                Id = role.Id,
                Name = name,
                UserCount = userCount,
                Permissions = RolePermissions.ForRoles([name])
            });
        }

        return items;
    }

    public async Task<AdminActionResult> SetAdminRoleAsync(
        string userId,
        bool isAdmin,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AdminActionResult.Fail("Kullanıcı bulunamadı.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminActionResult.Fail("Kullanıcı bulunamadı.");
        }

        var alreadyAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (alreadyAdmin == isAdmin)
        {
            return AdminActionResult.Success();
        }

        if (!isAdmin)
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count;
            if (adminCount <= 1)
            {
                return AdminActionResult.Fail(
                    "En az bir Admin kalmalıdır. Önce başka birine Admin verin.");
            }
        }

        IdentityResult roleResult = isAdmin
            ? await _userManager.AddToRoleAsync(user, AppRoles.Admin)
            : await _userManager.RemoveFromRoleAsync(user, AppRoles.Admin);

        if (!roleResult.Succeeded)
        {
            return AdminActionResult.Fail(
                roleResult.Errors.Select(error => error.Description).ToArray());
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.User))
        {
            await _userManager.AddToRoleAsync(user, AppRoles.User);
        }

        if (string.Equals(user.Id, actingUserId, StringComparison.Ordinal))
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        return AdminActionResult.Success();
    }
}
