namespace TablerAuth.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TablerAuth";

    public string Audience { get; set; } = "TablerAuth.Api";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public string SigningKey { get; set; } = string.Empty;
}
