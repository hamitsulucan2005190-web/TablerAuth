namespace TablerAuth.Application.Auth;

public sealed class RegisterRequest
{
    public required string Name { get; init; }

    public required string Email { get; init; }

    public required string Password { get; init; }
}
