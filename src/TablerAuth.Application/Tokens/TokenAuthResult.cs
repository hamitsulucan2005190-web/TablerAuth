namespace TablerAuth.Application.Tokens;

public sealed class TokenAuthResult
{
    public bool Succeeded { get; init; }

    public TokenResponse? Tokens { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static TokenAuthResult Success(TokenResponse tokens) =>
        new() { Succeeded = true, Tokens = tokens };

    public static TokenAuthResult Fail(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}
