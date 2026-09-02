namespace TablerAuth.Application.Auth;

public sealed class ApiLoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
