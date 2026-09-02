using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TablerAuth.Application.Auth;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            identity.AddClaim(new Claim("display_name", user.DisplayName));
        }

        var roles = await UserManager.GetRolesAsync(user);
        foreach (var permission in RolePermissions.ForRoles(roles))
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
        }

        return identity;
    }
}
