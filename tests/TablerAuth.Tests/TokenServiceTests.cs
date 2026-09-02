using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.DependencyInjection;
using TablerAuth.Application.Auth;
using TablerAuth.Application.Tokens;
using TablerAuth.Domain.Identity;
using TablerAuth.Infrastructure.Data;
using TablerAuth.Infrastructure.Identity;

namespace TablerAuth.Tests;

public class TokenServiceTests
{
    [Fact]
    public async Task Issue_puts_role_and_permission_claims_on_the_access_token()
    {
        await using var provider = IdentityTestHost.Create(nameof(Issue_puts_role_and_permission_claims_on_the_access_token));
        await IdentityTestHost.EnsureRolesAsync(provider);
        var user = await IdentityTestHost.CreateUserAsync(provider, "admin@test.local", AppRoles.Admin);

        var tokens = await provider.GetRequiredService<TokenService>().IssueTokensAsync(
            new TokenUser
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Roles = [AppRoles.Admin],
                Permissions = RolePermissions.ForRoles([AppRoles.Admin])
            },
            "127.0.0.1");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        Assert.Contains(jwt.Claims, claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value == AppRoles.Admin);
        Assert.Contains(jwt.Claims, claim => claim.Type == AppClaimTypes.Permission && claim.Value == AppPermissions.UsersManage);
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Refresh_rotates_the_refresh_token_and_reuse_revokes_the_family()
    {
        await using var provider = IdentityTestHost.Create(nameof(Refresh_rotates_the_refresh_token_and_reuse_revokes_the_family));
        await IdentityTestHost.EnsureRolesAsync(provider);
        var user = await IdentityTestHost.CreateUserAsync(provider, "api@test.local", AppRoles.User);
        var tokenService = provider.GetRequiredService<TokenService>();

        var issued = await tokenService.IssueTokensAsync(
            new TokenUser
            {
                Id = user.Id,
                Email = user.Email!,
                Roles = [AppRoles.User]
            },
            "127.0.0.1");

        var rotated = await tokenService.RefreshAsync(issued.RefreshToken, "127.0.0.1");
        Assert.True(rotated.Succeeded);
        Assert.NotNull(rotated.Tokens);
        Assert.NotEqual(issued.RefreshToken, rotated.Tokens.RefreshToken);

        var reuse = await tokenService.RefreshAsync(issued.RefreshToken, "127.0.0.1");
        Assert.False(reuse.Succeeded);

        var afterReuse = await tokenService.RefreshAsync(rotated.Tokens.RefreshToken, "127.0.0.1");
        Assert.False(afterReuse.Succeeded);

        var db = provider.GetRequiredService<ApplicationDbContext>();
        Assert.All(db.RefreshTokens.Where(token => token.UserId == user.Id), token => Assert.NotNull(token.RevokedAt));
    }
}
