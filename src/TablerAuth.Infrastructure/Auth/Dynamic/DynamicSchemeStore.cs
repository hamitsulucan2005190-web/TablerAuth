using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TablerAuth.Domain.Entities;
using TablerAuth.Infrastructure.Data;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// Etkin dış giriş sağlayıcılarını veritabanından okuyup secret store'daki gizli
/// değerlerle birleştirir ve kısa süreli önbellekte tutar. Singleton'dır; DbContext
/// scoped olduğu için her okumada kendi scope'unu açar.
/// </summary>
internal sealed class DynamicSchemeStore
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyDictionary<IdentityProviderKind, IExternalProviderBinding> _bindings;
    private readonly IReadOnlyList<IDynamicSchemeOptionsInvalidator> _invalidators;
    private readonly ILogger<DynamicSchemeStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ExternalProviderSnapshot _snapshot = ExternalProviderSnapshot.Empty;
    private Dictionary<string, string> _signatures = new(StringComparer.Ordinal);
    private long _version;
    private volatile bool _forceReload;

    public DynamicSchemeStore(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IEnumerable<IExternalProviderBinding> bindings,
        IEnumerable<IDynamicSchemeOptionsInvalidator> invalidators,
        ILogger<DynamicSchemeStore> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _bindings = bindings.ToDictionary(binding => binding.Kind);
        _invalidators = invalidators.ToList();
        _logger = logger;
    }

    /// <summary>
    /// En son yüklenen liste. Options oluşturucular senkron çalışmak zorunda olduğu için
    /// buradan okur; liste her zaman <see cref="GetAsync"/> tarafından önceden ısıtılır.
    /// </summary>
    public ExternalProviderSnapshot Current => _snapshot;

    public async Task<ExternalProviderSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = _snapshot;
        if (!_forceReload && snapshot.IsFresh(DateTimeOffset.UtcNow, CacheDuration))
        {
            return snapshot;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_forceReload && _snapshot.IsFresh(DateTimeOffset.UtcNow, CacheDuration))
            {
                return _snapshot;
            }

            _forceReload = false;
            return await ReloadAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Admin bir sağlayıcıyı değiştirdiğinde çağrılır; sonraki istek DB'den yeniden okur.</summary>
    public void Invalidate() => _forceReload = true;

    public bool IsHandlerAvailable(IdentityProviderKind kind) => _bindings.ContainsKey(kind);

    public IExternalProviderBinding? FindBinding(IdentityProviderKind kind) =>
        _bindings.TryGetValue(kind, out var binding) ? binding : null;

    private async Task<ExternalProviderSnapshot> ReloadAsync(CancellationToken cancellationToken)
    {
        List<IdentityProvider> rows;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            rows = await db.IdentityProviders
                .AsNoTracking()
                .Where(provider => provider.Enabled)
                .OrderBy(provider => provider.SortOrder)
                .ThenBy(provider => provider.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // DB geçici olarak erişilemezse giriş sayfası çökmesin; eldeki liste korunur.
            _logger.LogWarning(exception, "Identity providers could not be read; keeping the previously loaded list.");
            _snapshot = new ExternalProviderSnapshot
            {
                Version = _snapshot.Version,
                LoadedAt = DateTimeOffset.UtcNow,
                Providers = _snapshot.Providers
            };
            return _snapshot;
        }

        var providers = new Dictionary<string, ResolvedExternalProvider>(StringComparer.Ordinal);
        var signatures = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var binding = FindBinding(row.Kind);
            if (binding is null)
            {
                _logger.LogDebug(
                    "Provider {Scheme} is enabled but no handler is installed for kind {Kind}; it stays hidden.",
                    row.Scheme,
                    row.Kind);
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.ClientId))
            {
                _logger.LogDebug("Provider {Scheme} has no ClientId; it stays hidden.", row.Scheme);
                continue;
            }

            var clientSecret = _configuration[row.ClientSecretKey];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogDebug(
                    "Provider {Scheme} has no secret under configuration key {Key}; it stays hidden.",
                    row.Scheme,
                    row.ClientSecretKey);
                continue;
            }

            if (binding.RequiresAuthority && string.IsNullOrWhiteSpace(row.Authority))
            {
                _logger.LogDebug("Provider {Scheme} needs an Authority value; it stays hidden.", row.Scheme);
                continue;
            }

            var callbackPath = string.IsNullOrWhiteSpace(row.CallbackPath)
                ? binding.DefaultCallbackPath
                : row.CallbackPath!.Trim();

            var scopes = SplitScopes(row.Scopes);
            if (scopes.Count == 0)
            {
                scopes = SplitScopes(binding.DefaultScopes);
            }

            providers[row.Scheme] = new ResolvedExternalProvider
            {
                Scheme = row.Scheme,
                DisplayName = row.DisplayName,
                Kind = row.Kind,
                HandlerType = binding.HandlerType,
                ClientId = row.ClientId,
                ClientSecret = clientSecret,
                CallbackPath = callbackPath,
                Scopes = scopes,
                Authority = row.Authority,
                Signature = ComputeSignature(row, binding, callbackPath, scopes, clientSecret)
            };

            signatures[row.Scheme] = providers[row.Scheme].Signature;
        }

        var changedSchemes = signatures
            .Where(entry => !_signatures.TryGetValue(entry.Key, out var previous) || previous != entry.Value)
            .Select(entry => entry.Key)
            .Concat(_signatures.Keys.Where(scheme => !signatures.ContainsKey(scheme)))
            .ToList();

        foreach (var scheme in changedSchemes)
        {
            foreach (var invalidator in _invalidators)
            {
                invalidator.Invalidate(scheme);
            }
        }

        var version = changedSchemes.Count > 0
            ? Interlocked.Increment(ref _version)
            : _snapshot.Version;

        if (changedSchemes.Count > 0)
        {
            _logger.LogInformation(
                "External sign-in providers reloaded: {Schemes}",
                providers.Count == 0 ? "(none)" : string.Join(", ", providers.Keys));
        }

        _signatures = signatures;
        _snapshot = new ExternalProviderSnapshot
        {
            Version = version,
            LoadedAt = DateTimeOffset.UtcNow,
            Providers = providers
        };

        return _snapshot;
    }

    private static List<string> SplitScopes(string? scopes) =>
        string.IsNullOrWhiteSpace(scopes)
            ? new List<string>()
            : scopes.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToList();

    private static string ComputeSignature(
        IdentityProvider row,
        IExternalProviderBinding binding,
        string callbackPath,
        IReadOnlyList<string> scopes,
        string clientSecret)
    {
        var raw = string.Join(
            '\n',
            row.DisplayName,
            row.Kind.ToString(),
            binding.HandlerType.FullName,
            row.ClientId,
            row.Authority ?? string.Empty,
            callbackPath,
            string.Join(' ', scopes),
            clientSecret);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
