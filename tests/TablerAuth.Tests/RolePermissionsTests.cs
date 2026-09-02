using TablerAuth.Application.Auth;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Tests;

public class RolePermissionsTests
{
    [Fact]
    public void Admin_role_grants_all_three_permissions()
    {
        var permissions = RolePermissions.ForRoles([AppRoles.Admin]);

        Assert.Equal(
            new[]
            {
                AppPermissions.ProvidersManage,
                AppPermissions.RolesView,
                AppPermissions.UsersManage
            },
            permissions);
    }

    [Fact]
    public void User_role_grants_no_permissions()
    {
        var permissions = RolePermissions.ForRoles([AppRoles.User]);

        Assert.Empty(permissions);
    }

    [Fact]
    public void Admin_and_User_together_still_grant_admin_permissions()
    {
        var permissions = RolePermissions.ForRoles([AppRoles.User, AppRoles.Admin]);

        Assert.Equal(3, permissions.Count);
        Assert.Contains(AppPermissions.UsersManage, permissions);
    }
}
