using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TablerAuth.Application.IdentityProviders;
using TablerAuth.Domain.Entities;
using TablerAuth.Infrastructure.Auth.Dynamic;
using TablerAuth.Infrastructure.Data;

namespace TablerAuth.Infrastructure.Identity;

internal class IdentityProviderService : IIdentityProviderService
{
    /// <summary>Identity ve JWT'nin kendi scheme adları; bunlar ezilemez.</summary>
    private static readonly string[] ReservedSchemes =
    [
        IdentityConstants.ApplicationScheme,
        IdentityConstants.ExternalScheme,
        IdentityConstants.TwoFactorRememberMeScheme,
        IdentityConstants.TwoFactorUserIdScheme,
        JwtBearerDefaults.AuthenticationScheme
    ];

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly DynamicSchemeStore _schemeStore;

    public IdentityProviderService(
        ApplicationDbContext db,
        IConfiguration configuration,
        DynamicSchemeStore schemeStore)
    {
        _db = db;
        _configuration = configuration;
        _schemeStore = schemeStore;
    }

    public async Task<IReadOnlyList<IdentityProviderListItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.IdentityProviders
            .AsNoTracking()
            .OrderBy(provider => provider.SortOrder)
            .ThenBy(provider => provider.DisplayName)
            .ToListAsync(cancellationToken);

        var active = (await _schemeStore.GetAsync(cancellationToken)).Providers;

        return rows.Select(row =>
        {
            var binding = _schemeStore.FindBinding(row.Kind);
            var hasSecret = !string.IsNullOrWhiteSpace(_configuration[row.ClientSecretKey]);
            var isActive = active.ContainsKey(row.Scheme);

            return new IdentityProviderListItem
            {
                Id = row.Id,
                Scheme = row.Scheme,
                DisplayName = row.DisplayName,
                Kind = row.Kind.ToString(),
                ClientIdPreview = Preview(row.ClientId),
                ClientSecretKey = row.ClientSecretKey,
                HasClientSecret = hasSecret,
                Authority = row.Authority,
                Scopes = string.IsNullOrWhiteSpace(row.Scopes) ? binding?.DefaultScopes : row.Scopes,
                CallbackPath = EffectiveCallbackPath(row, binding),
                Enabled = row.Enabled,
                IsActive = isActive,
                Warning = DescribeWarning(row, binding is not null, binding?.RequiresAuthority == true, hasSecret, isActive)
            };
        }).ToList();
    }

    public async Task<IdentityProviderInput?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _db.IdentityProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(provider => provider.Id == id, cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new IdentityProviderInput
        {
            Id = row.Id,
            Scheme = row.Scheme,
            DisplayName = row.DisplayName,
            Kind = row.Kind.ToString(),
            ClientId = row.ClientId,
            ClientSecretKey = row.ClientSecretKey,
            Authority = row.Authority,
            Scopes = row.Scopes,
            CallbackPath = row.CallbackPath,
            Enabled = row.Enabled,
            SortOrder = row.SortOrder
        };
    }

    public IReadOnlyList<IdentityProviderKindOption> ListKinds() =>
        Enum.GetValues<IdentityProviderKind>()
            .Select(kind =>
            {
                var binding = _schemeStore.FindBinding(kind);
                return new IdentityProviderKindOption
                {
                    Value = kind.ToString(),
                    HandlerInstalled = binding is not null,
                    DefaultCallbackPath = binding?.DefaultCallbackPath,
                    DefaultScopes = binding?.DefaultScopes,
                    RequiresAuthority = binding?.RequiresAuthority ?? false,
                    DefaultScheme = kind.ToString(),
                    DefaultSecretKey = $"Authentication:{kind}:ClientSecret"
                };
            })
            .Where(kind => kind.HandlerInstalled)
            .ToList();

    public async Task<IdentityProviderSaveResult> SaveAsync(
        IdentityProviderInput input,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<IdentityProviderKind>(input.Kind, ignoreCase: true, out var kind))
        {
            return IdentityProviderSaveResult.Fail($"\"{input.Kind}\" bilinen bir sağlayıcı türü değil.");
        }

        var binding = _schemeStore.FindBinding(kind);
        if (binding is null)
        {
            return IdentityProviderSaveResult.Fail(
                $"Bu uygulama {kind} ile henüz konuşmayı bilmiyor. Önce paketini ekleyin.");
        }

        var scheme = input.Scheme.Trim();
        if (ReservedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
        {
            return IdentityProviderSaveResult.Fail($"\"{scheme}\" Identity veya JWT tarafından ayrılmıştır.");
        }

        if (SecretStoreKeyRules.LooksLikePastedSecret(input.ClientSecretKey))
        {
            return IdentityProviderSaveResult.Fail(SecretStoreKeyRules.PastedSecretMessage);
        }

        var authority = Normalize(input.Authority);
        if (binding.RequiresAuthority && authority is null)
        {
            return IdentityProviderSaveResult.Fail($"{kind} için bir Authority değeri gerekir.");
        }

        var callbackPath = Normalize(input.CallbackPath) ?? binding.DefaultCallbackPath;

        var others = await _db.IdentityProviders
            .Where(provider => provider.Id != (input.Id ?? 0))
            .ToListAsync(cancellationToken);

        if (others.Any(other => string.Equals(other.Scheme, scheme, StringComparison.OrdinalIgnoreCase)))
        {
            return IdentityProviderSaveResult.Fail($"Başka bir sağlayıcı \"{scheme}\" iç adını zaten kullanıyor.");
        }

        // İki sağlayıcı aynı callback yolunu paylaşırsa istek yanlış handler'a düşer.
        var callbackOwner = others.FirstOrDefault(other =>
            string.Equals(
                EffectiveCallbackPath(other, _schemeStore.FindBinding(other.Kind)),
                callbackPath,
                StringComparison.OrdinalIgnoreCase));

        if (callbackOwner is not null)
        {
            return IdentityProviderSaveResult.Fail(
                $"\"{callbackPath}\" dönüş yolu zaten \"{callbackOwner.Scheme}\" tarafından kullanılıyor.");
        }

        IdentityProvider row;
        if (input.Id is null)
        {
            row = new IdentityProvider
            {
                Scheme = scheme,
                DisplayName = input.DisplayName.Trim(),
                ClientId = input.ClientId.Trim(),
                ClientSecretKey = input.ClientSecretKey.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.IdentityProviders.Add(row);
        }
        else
        {
            var existing = await _db.IdentityProviders
                .FirstOrDefaultAsync(provider => provider.Id == input.Id, cancellationToken);

            if (existing is null)
            {
                return IdentityProviderSaveResult.Fail("Bu sağlayıcı artık yok.");
            }

            row = existing;
            row.Scheme = scheme;
            row.DisplayName = input.DisplayName.Trim();
            row.ClientId = input.ClientId.Trim();
            row.ClientSecretKey = input.ClientSecretKey.Trim();
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        row.Kind = kind;
        row.Authority = authority;
        row.Scopes = Normalize(input.Scopes);
        row.CallbackPath = callbackPath;
        row.Enabled = input.Enabled;
        row.SortOrder = input.SortOrder;

        await _db.SaveChangesAsync(cancellationToken);
        _schemeStore.Invalidate();

        return IdentityProviderSaveResult.Success();
    }

    public async Task<bool> SetEnabledAsync(
        int id,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var provider = await _db.IdentityProviders
            .FirstOrDefaultAsync(row => row.Id == id, cancellationToken);

        if (provider is null)
        {
            return false;
        }

        if (provider.Enabled != enabled)
        {
            provider.Enabled = enabled;
            provider.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _schemeStore.Invalidate();
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _db.IdentityProviders
            .FirstOrDefaultAsync(row => row.Id == id, cancellationToken);

        if (provider is null)
        {
            return false;
        }

        _db.IdentityProviders.Remove(provider);
        await _db.SaveChangesAsync(cancellationToken);
        _schemeStore.Invalidate();

        return true;
    }

    private static string EffectiveCallbackPath(IdentityProvider row, IExternalProviderBinding? binding) =>
        string.IsNullOrWhiteSpace(row.CallbackPath)
            ? binding?.DefaultCallbackPath ?? "—"
            : row.CallbackPath;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Preview(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return "—";
        }

        return clientId.Length <= 12 ? clientId : $"{clientId[..12]}…";
    }

    private static string? DescribeWarning(
        IdentityProvider row,
        bool hasBinding,
        bool requiresAuthority,
        bool hasSecret,
        bool isActive)
    {
        if (!hasBinding)
        {
            return $"Bu uygulama {row.Kind} ile henüz konuşmayı bilmiyor, bu yüzden giriş düğmesi görünemez.";
        }

        // İkisi de eksikken tek tek göstermek yanıltıcı oluyordu: App ID girildikten
        // sonra kullanıcı ikinci engelle karşılaşıyordu. Hepsi bir arada listeleniyor.
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(row.ClientId))
        {
            missing.Add("sağlayıcının geliştirici konsolundan kopyaladığınız uygulama kimliği");
        }

        if (!hasSecret)
        {
            missing.Add($"gizli — User Secrets, ortam değişkeni veya Key Vault’ta “{row.ClientSecretKey}” altına konur");
        }

        if (requiresAuthority && string.IsNullOrWhiteSpace(row.Authority))
        {
            missing.Add("Authority adresi (OpenID discovery, örneğin https://accounts.google.com)");
        }

        if (missing.Count > 0)
        {
            return "Giriş düğmesi, "
                + string.Join(" ve ", missing)
                + " eklenene kadar gizli kalır. Sağlayıcıyı açmak tek başına yetmez.";
        }

        if (!row.Enabled || isActive)
        {
            return null;
        }

        return "Burada açık, ama giriş sayfasında henüz yok. Birkaç saniye bekleyip yenileyin.";
    }
}
