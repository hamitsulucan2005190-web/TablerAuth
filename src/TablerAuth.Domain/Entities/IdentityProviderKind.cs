namespace TablerAuth.Domain.Entities;

/// <summary>
/// Hangi authentication handler'ın kullanılacağını belirler. DB'ye satır eklemek
/// tek başına yetmez; o satırın türü için bir handler kurulu olmak zorundadır.
/// </summary>
public enum IdentityProviderKind
{
    Google = 1,
    /// <summary>Kullanılmıyor. Eski satırların Kind=2 değeri kaymasın diye duruyor.</summary>
    Facebook = 2,
    GitHub = 3,
    LinkedIn = 4,
    /// <summary>
    /// Generic OpenID Connect. Authority (discovery URL) zorunlu; Google/GitHub/LinkedIn
    /// hazır handler'larından ayrı bir paket ve options kurulumu kullanır.
    /// </summary>
    Oidc = 5
}
