using System.Security.Claims;
using TablerAuth.Application.Tokens;

namespace TablerAuth.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<SignedInUserInfo?> GetSignedInUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<TokenAuthResult> ApiLoginAsync(
        ApiLoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalProvider>> GetExternalProvidersAsync(CancellationToken cancellationToken = default);

    Task<ExternalLoginChallenge?> CreateExternalLoginChallengeAsync(
        string scheme,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<AuthResult> ExternalLoginAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<ForgotPasswordResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}
