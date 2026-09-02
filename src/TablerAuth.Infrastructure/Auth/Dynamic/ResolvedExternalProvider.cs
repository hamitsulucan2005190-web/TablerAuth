using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// DB satırı + secret store'dan okunan gizli değer birleştirilmiş, kullanıma hazır sağlayıcı.
/// Yalnızca bellekte durur; loglanmaz.
/// </summary>
internal sealed class ResolvedExternalProvider
{
    public required string Scheme { get; init; }

    public required string DisplayName { get; init; }

    public required IdentityProviderKind Kind { get; init; }

    public required Type HandlerType { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }

    public required string CallbackPath { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }

    public string? Authority { get; init; }

    /// <summary>
    /// Tüm alanların özeti. Değişince scheme yeniden kaydedilir ve options önbelleği temizlenir.
    /// Gizli değer düz metin değil, hash olarak katkı verir.
    /// </summary>
    public required string Signature { get; init; }
}
