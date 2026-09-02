namespace TablerAuth.Application.Auth;

public sealed class SignInMethodInfo
{
    public required string Scheme { get; init; }

    public required string DisplayName { get; init; }

    public bool IsExternal { get; init; }

    public bool UsedThisSession { get; init; }
}
