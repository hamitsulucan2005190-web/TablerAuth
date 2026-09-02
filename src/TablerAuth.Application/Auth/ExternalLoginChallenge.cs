namespace TablerAuth.Application.Auth;

/// <summary>
/// Harici sağlayıcıya yönlendirme için gereken veri. Identity'nin beklediği
/// anahtarları Infrastructure doldurur; Web katmanı yalnızca taşır.
/// </summary>
public sealed class ExternalLoginChallenge
{
    public required string Scheme { get; init; }

    public required string RedirectUri { get; init; }

    public IReadOnlyDictionary<string, string?> Items { get; init; } =
        new Dictionary<string, string?>();
}
