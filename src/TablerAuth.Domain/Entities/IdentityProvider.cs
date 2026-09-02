namespace TablerAuth.Domain.Entities;

/// <summary>
/// Bir dış giriş sağlayıcısının veritabanındaki konfigürasyonu.
/// ClientSecret burada tutulmaz; yalnızca secret store'daki anahtarın adı tutulur.
/// </summary>
public class IdentityProvider
{
    public int Id { get; set; }

    /// <summary>Authentication scheme adı, ör. "Google". Callback yolunu da bu belirler.</summary>
    public required string Scheme { get; set; }

    /// <summary>Login sayfasındaki buton yazısı.</summary>
    public required string DisplayName { get; set; }

    public IdentityProviderKind Kind { get; set; }

    public required string ClientId { get; set; }

    /// <summary>
    /// Secret'ın kendisi değil, secret store'daki anahtar adı
    /// (ör. "Authentication:Google:ClientSecret"). Değer User Secrets / env / Key Vault'tan okunur.
    /// </summary>
    public required string ClientSecretKey { get; set; }

    /// <summary>Generic OIDC sağlayıcıları için discovery adresi. Google gibi hazır handler'larda boş kalır.</summary>
    public string? Authority { get; set; }

    /// <summary>Boşlukla ayrılmış scope listesi. Boşsa handler'ın varsayılanı kullanılır.</summary>
    public string? Scopes { get; set; }

    /// <summary>Sağlayıcıya bildirilen dönüş yolu, ör. "/signin-google". Boşsa handler varsayılanı kullanılır.</summary>
    public string? CallbackPath { get; set; }

    public bool Enabled { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
