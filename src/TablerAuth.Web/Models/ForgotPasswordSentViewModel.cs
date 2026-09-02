namespace TablerAuth.Web.Models;

public class ForgotPasswordSentViewModel
{
    public required string Email { get; init; }

    public string? DevelopmentResetUrl { get; init; }

    public bool DevelopmentEmailSent { get; init; }
}
