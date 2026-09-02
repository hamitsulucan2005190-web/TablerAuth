namespace TablerAuth.Infrastructure.Auth.Dynamic;

internal sealed class ExternalProviderSnapshot
{
    public static readonly ExternalProviderSnapshot Empty = new()
    {
        Version = 0,
        LoadedAt = DateTimeOffset.MinValue,
        Providers = new Dictionary<string, ResolvedExternalProvider>(StringComparer.Ordinal)
    };

    /// <summary>Yalnızca içerik gerçekten değiştiğinde artar; boşa scheme/option yenilemesi olmasın.</summary>
    public required long Version { get; init; }

    public required DateTimeOffset LoadedAt { get; init; }

    public required IReadOnlyDictionary<string, ResolvedExternalProvider> Providers { get; init; }

    public bool IsFresh(DateTimeOffset now, TimeSpan ttl) => now - LoadedAt < ttl;
}
