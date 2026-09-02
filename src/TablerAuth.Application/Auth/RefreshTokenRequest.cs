namespace TablerAuth.Application.Auth;

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
