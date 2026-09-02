namespace TablerAuth.Application.Tokens;

public interface ITokenService
{
    Task<TokenResponse> IssueTokensAsync(
        TokenUser user,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<TokenAuthResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
