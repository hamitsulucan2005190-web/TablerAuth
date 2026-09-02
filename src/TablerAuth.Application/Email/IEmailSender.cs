namespace TablerAuth.Application.Email;

public interface IEmailSender
{
    /// <summary>
    /// True when the message was handed to SMTP. False if SMTP is not configured or send failed.
    /// </summary>
    Task<bool> SendPasswordResetAsync(string to, string resetUrl, CancellationToken cancellationToken = default);
}
