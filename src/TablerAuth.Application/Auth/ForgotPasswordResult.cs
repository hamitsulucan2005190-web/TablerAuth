namespace TablerAuth.Application.Auth;

public sealed class ForgotPasswordResult
{
    /// <summary>
    /// Present only when a matching account exists and a development token was requested.
    /// Null otherwise so the UI always shows the same public message.
    /// </summary>
    public string? ResetToken { get; init; }

    /// <summary>
    /// True when SMTP accepted the reset message. False if the account was missing,
    /// SMTP is not configured, or the send failed.
    /// </summary>
    public bool EmailSent { get; init; }
}
