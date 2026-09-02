namespace TablerAuth.Application.Auth;

public sealed class ExternalProvider
{
    public required string Scheme { get; init; }

    public required string DisplayName { get; init; }
}
