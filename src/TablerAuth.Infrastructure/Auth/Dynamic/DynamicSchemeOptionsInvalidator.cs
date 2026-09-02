using Microsoft.Extensions.Options;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// Bir sağlayıcının DB kaydı değişince ilgili options nesnesi önbellekten atılmalı,
/// yoksa handler eski ClientId/secret ile çalışmaya devam eder.
/// </summary>
internal interface IDynamicSchemeOptionsInvalidator
{
    void Invalidate(string scheme);
}

internal sealed class DynamicSchemeOptionsInvalidator<TOptions> : IDynamicSchemeOptionsInvalidator
    where TOptions : class
{
    private readonly IOptionsMonitorCache<TOptions> _cache;

    public DynamicSchemeOptionsInvalidator(IOptionsMonitorCache<TOptions> cache)
    {
        _cache = cache;
    }

    public void Invalidate(string scheme) => _cache.TryRemove(scheme);
}
