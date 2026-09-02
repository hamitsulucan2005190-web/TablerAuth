namespace TablerAuth.Application.Auth;

public sealed class SignedInUserInfo
{
    public required string Email { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Permissions { get; init; } = [];

    public required SignInMethodInfo CurrentSignIn { get; init; }

    public IReadOnlyList<SignInMethodInfo> LinkedMethods { get; init; } = [];

    public bool HasPassword { get; init; }
}
