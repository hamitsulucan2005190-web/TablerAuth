namespace TablerAuth.Application.Auth;

public sealed class AuthResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static AuthResult Success() => new() { Succeeded = true };

    public static AuthResult Fail(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}
