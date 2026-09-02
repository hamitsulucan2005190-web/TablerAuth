using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// Startup'ta sabitlenen scheme listesine, veritabanındaki etkin sağlayıcıları
/// çalışma zamanında ekler. Böylece yeni bir sağlayıcı için uygulamayı yeniden
/// başlatmak gerekmez; kapatılan sağlayıcının callback yolu da handler listesinden düşer.
/// </summary>
internal sealed class DynamicAuthenticationSchemeProvider : AuthenticationSchemeProvider
{
    private readonly DynamicSchemeStore _store;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _applied = new(StringComparer.Ordinal);
    private long _appliedVersion = -1;

    public DynamicAuthenticationSchemeProvider(IOptions<AuthenticationOptions> options, DynamicSchemeStore store)
        : base(options)
    {
        _store = store;
    }

    public override async Task<AuthenticationScheme?> GetSchemeAsync(string name)
    {
        var scheme = await base.GetSchemeAsync(name);
        if (scheme is not null)
        {
            return scheme;
        }

        await SyncAsync();
        return await base.GetSchemeAsync(name);
    }

    public override async Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
    {
        await SyncAsync();
        return await base.GetAllSchemesAsync();
    }

    public override async Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
    {
        await SyncAsync();
        return await base.GetRequestHandlerSchemesAsync();
    }

    private async Task SyncAsync()
    {
        var snapshot = await _store.GetAsync();
        if (Interlocked.Read(ref _appliedVersion) == snapshot.Version)
        {
            return;
        }

        lock (_sync)
        {
            if (_appliedVersion == snapshot.Version)
            {
                return;
            }

            foreach (var scheme in _applied.Keys.ToList())
            {
                var stillWanted = snapshot.Providers.TryGetValue(scheme, out var provider)
                    && provider.Signature == _applied[scheme];

                if (stillWanted)
                {
                    continue;
                }

                base.RemoveScheme(scheme);
                _applied.Remove(scheme);
            }

            foreach (var provider in snapshot.Providers.Values)
            {
                if (_applied.ContainsKey(provider.Scheme))
                {
                    continue;
                }

                var added = base.TryAddScheme(
                    new AuthenticationScheme(provider.Scheme, provider.DisplayName, provider.HandlerType));

                if (added)
                {
                    _applied[provider.Scheme] = provider.Signature;
                }
            }

            Interlocked.Exchange(ref _appliedVersion, snapshot.Version);
        }
    }
}
