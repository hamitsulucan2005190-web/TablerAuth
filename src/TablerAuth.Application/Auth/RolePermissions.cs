using TablerAuth.Domain.Identity;

namespace TablerAuth.Application.Auth;

/// <summary>
/// Rol → izin eşlemesi. Ayrı permission tablosu yok; Admin üç izni alır, User hiçbirini almaz.
/// </summary>
public static class RolePermissions
{
    public static IReadOnlyList<string> ForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
        {
            if (string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add(AppPermissions.UsersManage);
                permissions.Add(AppPermissions.RolesView);
                permissions.Add(AppPermissions.ProvidersManage);
            }
        }

        return permissions.OrderBy(permission => permission, StringComparer.Ordinal).ToArray();
    }
}
