namespace TablerAuth.Application.IdentityProviders;

public sealed class IdentityProviderListItem
{
    public required int Id { get; init; }

    public required string Scheme { get; init; }

    public required string DisplayName { get; init; }

    public required string Kind { get; init; }

    /// <summary>ClientId kısaltılmış gösterilir; tam değer ekrana basılmaz.</summary>
    public required string ClientIdPreview { get; init; }

    /// <summary>Secret'ın kendisi değil, secret store'daki anahtarın adı.</summary>
    public required string ClientSecretKey { get; init; }

    public required bool HasClientSecret { get; init; }

    public string? Authority { get; init; }

    public string? Scopes { get; init; }

    public required string CallbackPath { get; init; }

    /// <summary>Admin'in DB'de açtığı/kapattığı bayrak.</summary>
    public required bool Enabled { get; init; }

    /// <summary>Gerçekten authentication scheme olarak kayıtlı mı, yani giriş sayfasında görünüyor mu.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Enabled olmasına rağmen aktif değilse sebebi.</summary>
    public string? Warning { get; init; }
}
