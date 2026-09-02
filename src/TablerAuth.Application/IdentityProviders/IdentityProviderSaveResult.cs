namespace TablerAuth.Application.IdentityProviders;

public sealed class IdentityProviderSaveResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static IdentityProviderSaveResult Success() => new() { Succeeded = true };

    public static IdentityProviderSaveResult Fail(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}
