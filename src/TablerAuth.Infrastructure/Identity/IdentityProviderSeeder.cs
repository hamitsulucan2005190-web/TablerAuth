using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TablerAuth.Domain.Entities;
using TablerAuth.Infrastructure.Data;

namespace TablerAuth.Infrastructure.Identity;

/// <summary>
/// Üç sağlayıcı satırını veritabanına yazar. ClientId gizli bilgi değil, satırda tutulur;
/// ClientSecret için yalnızca secret store anahtarının adı yazılır.
/// Facebook ve Microsoft hesaplarına girilemediği için seed edilmez; eski satırlar silinir.
/// </summary>
public static class IdentityProviderSeeder
{
    private static readonly string[] RetiredSchemes = ["Microsoft", "Facebook"];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await RemoveRetiredRowsAsync(db, logger, cancellationToken);

        var specs = new[]
        {
            new ProviderSeed(
                Scheme: "Google",
                DisplayName: "Google",
                Kind: IdentityProviderKind.Google,
                ClientIdKey: "Authentication:Google:ClientId",
                ClientSecretKey: "Authentication:Google:ClientSecret",
                Scopes: "openid profile email",
                CallbackPath: "/signin-google",
                SortOrder: 1),
            new ProviderSeed(
                Scheme: "GitHub",
                DisplayName: "GitHub",
                Kind: IdentityProviderKind.GitHub,
                ClientIdKey: "Authentication:GitHub:ClientId",
                ClientSecretKey: "Authentication:GitHub:ClientSecret",
                Scopes: "read:user user:email",
                CallbackPath: "/signin-github",
                SortOrder: 2),
            new ProviderSeed(
                Scheme: "LinkedIn",
                DisplayName: "LinkedIn",
                Kind: IdentityProviderKind.LinkedIn,
                ClientIdKey: "Authentication:LinkedIn:ClientId",
                ClientSecretKey: "Authentication:LinkedIn:ClientSecret",
                Scopes: "openid profile email",
                CallbackPath: "/signin-linkedin",
                SortOrder: 3)
        };

        foreach (var spec in specs)
        {
            await EnsureAsync(db, configuration, logger, spec, cancellationToken);
        }
    }

    private static async Task RemoveRetiredRowsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var retired = await db.IdentityProviders
            .Where(provider => RetiredSchemes.Contains(provider.Scheme))
            .ToListAsync(cancellationToken);

        if (retired.Count == 0)
        {
            return;
        }

        db.IdentityProviders.RemoveRange(retired);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Removed retired identity provider rows: {Schemes}.",
            string.Join(", ", retired.Select(provider => provider.Scheme)));
    }

    private static async Task EnsureAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger,
        ProviderSeed spec,
        CancellationToken cancellationToken)
    {
        var existing = await db.IdentityProviders
            .FirstOrDefaultAsync(provider => provider.Scheme == spec.Scheme, cancellationToken);

        var clientId = configuration[spec.ClientIdKey];
        var hasSecret = !string.IsNullOrWhiteSpace(configuration[spec.ClientSecretKey]);

        if (existing is null)
        {
            db.IdentityProviders.Add(new IdentityProvider
            {
                Scheme = spec.Scheme,
                DisplayName = spec.DisplayName,
                Kind = spec.Kind,
                ClientId = clientId ?? string.Empty,
                ClientSecretKey = spec.ClientSecretKey,
                Scopes = spec.Scopes,
                CallbackPath = spec.CallbackPath,
                Enabled = !string.IsNullOrWhiteSpace(clientId) && hasSecret,
                SortOrder = spec.SortOrder,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seeded the {Scheme} identity provider row (enabled: {Enabled}).",
                spec.Scheme,
                !string.IsNullOrWhiteSpace(clientId) && hasSecret);
            return;
        }

        if (string.IsNullOrWhiteSpace(existing.ClientId) && !string.IsNullOrWhiteSpace(clientId))
        {
            existing.ClientId = clientId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            // İlk kez App ID gelince, secret de hazırsa butonu aç. Admin sonradan
            // kapatırsa buraya tekrar düşülmez çünkü ClientId artık dolu.
            if (hasSecret && !existing.Enabled)
            {
                existing.Enabled = true;
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Filled {Scheme} ClientId from configuration (enabled: {Enabled}).",
                spec.Scheme,
                existing.Enabled);
        }
    }

    private sealed record ProviderSeed(
        string Scheme,
        string DisplayName,
        IdentityProviderKind Kind,
        string ClientIdKey,
        string ClientSecretKey,
        string Scopes,
        string CallbackPath,
        int SortOrder);
}
