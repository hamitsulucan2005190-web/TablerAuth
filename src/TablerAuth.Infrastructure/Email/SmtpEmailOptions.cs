namespace TablerAuth.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && Port > 0
        && !string.IsNullOrWhiteSpace(User)
        && !string.IsNullOrWhiteSpace(Password);
}
