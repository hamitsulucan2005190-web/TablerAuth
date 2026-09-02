namespace TablerAuth.Application.Auth;

public sealed class ForgotPasswordRequest
{
    public required string Email { get; init; }

    /// <summary>
    /// Builds the public reset URL from the raw Identity token. The token
    /// must be query-encoded by the caller; never log it.
    /// </summary>
    public required Func<string, string> CreateResetLink { get; init; }

    /// <summary>
    /// Development only: when true and the email matches an account,
    /// a reset token is returned so the UI can show a fallback link. Never log it.
    /// </summary>
    public bool IncludeResetToken { get; init; }
}
