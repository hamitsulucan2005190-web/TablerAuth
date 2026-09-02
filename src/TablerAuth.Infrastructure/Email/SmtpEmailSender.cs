using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TablerAuth.Application.Email;

namespace TablerAuth.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetAsync(
        string to,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Password reset email was skipped because Email:Smtp User/Password are not set. " +
                "Set them with User Secrets (not appsettings.json).");
            return false;
        }

        var from = string.IsNullOrWhiteSpace(_options.From) ? _options.User : _options.From;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = "TablerAuth şifrenizi sıfırlayın";
        message.Body = new TextPart("plain")
        {
            Text =
                "Yeni şifre belirlemek için bu bağlantıya tıklayın:" +
                Environment.NewLine +
                Environment.NewLine +
                resetUrl +
                Environment.NewLine +
                Environment.NewLine +
                "Bunu siz istemediyseniz bu e-postayı yok sayın."
        };

        try
        {
            using var client = new SmtpClient();
            // MailKit checks CRLs by default. On macOS that lookup often fails with
            // "An incomplete certificate revocation check occurred" even for a valid
            // smtp.gmail.com certificate, so the message never leaves the machine.
            client.CheckCertificateRevocation = false;

            var socketOptions = _options.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
            await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset email could not be sent.");
            return false;
        }
    }
}
