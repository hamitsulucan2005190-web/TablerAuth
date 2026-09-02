using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TablerAuth.Application.Auth;
using TablerAuth.Application.Tokens;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;
using TablerAuth.Infrastructure.Auth;
using TablerAuth.Infrastructure.Data;

namespace TablerAuth.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwt;

    public TokenService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _userManager = userManager;
        _jwt = jwtOptions.Value;
    }

    public async Task<TokenResponse> IssueTokensAsync(
        TokenUser user,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var accessToken = CreateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, cancellationToken);
        return ToResponse(accessToken, refreshToken);
    }

    public async Task<TokenAuthResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return TokenAuthResult.Fail("Yenileme jetonu geçersiz.");
        }

        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
        {
            return TokenAuthResult.Fail("Yenileme jetonu geçersiz.");
        }

        if (stored.RevokedAt is not null)
        {
            await RevokeAllActiveTokensAsync(stored.UserId, ipAddress, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return TokenAuthResult.Fail("Yenileme jetonu geçersiz.");
        }

        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return TokenAuthResult.Fail("Yenileme jetonu geçersiz.");
        }

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            stored.RevokedByIp = ipAddress;
            await _db.SaveChangesAsync(cancellationToken);
            return TokenAuthResult.Fail("Yenileme jetonu geçersiz.");
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        var tokenUser = new TokenUser
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles,
            Permissions = RolePermissions.ForRoles(roles)
        };

        var newRefresh = CreateRefreshTokenValue();
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.RevokedByIp = ipAddress;
        stored.ReplacedByTokenHash = HashToken(newRefresh);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = stored.ReplacedByTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        });

        await _db.SaveChangesAsync(cancellationToken);

        return TokenAuthResult.Success(ToResponse(CreateAccessToken(tokenUser), newRefresh));
    }

    public async Task RevokeAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null)
        {
            return;
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.RevokedByIp = ipAddress;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string CreateAccessToken(TokenUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in user.Permissions.Count > 0
                     ? user.Permissions
                     : RolePermissions.ForRoles(user.Roles))
        {
            claims.Add(new Claim(AppClaimTypes.Permission, permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var value = CreateRefreshTokenValue();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(value),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        });

        await _db.SaveChangesAsync(cancellationToken);
        return value;
    }

    private async Task RevokeAllActiveTokensAsync(
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = ipAddress;
        }
    }

    private TokenResponse ToResponse(string accessToken, string refreshToken) =>
        new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = _jwt.AccessTokenMinutes * 60
        };

    private static string CreateRefreshTokenValue()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
