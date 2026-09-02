namespace TablerAuth.Application.IdentityProviders;

/// <summary>Sağlayıcı ekleme/düzenleme girdisi. Client secret burada taşınmaz.</summary>
public sealed class IdentityProviderInput
{
    /// <summary>Yeni kayıtta null.</summary>
    public int? Id { get; init; }

    public required string Scheme { get; init; }

    public required string DisplayName { get; init; }

    public required string Kind { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecretKey { get; init; }

    public string? Authority { get; init; }

    public string? Scopes { get; init; }

    public string? CallbackPath { get; init; }

    public bool Enabled { get; init; }

    public int SortOrder { get; init; }
}
