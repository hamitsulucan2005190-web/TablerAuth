namespace TablerAuth.Application.Tokens;

public sealed class TokenUser
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Permissions { get; init; } = [];
}
