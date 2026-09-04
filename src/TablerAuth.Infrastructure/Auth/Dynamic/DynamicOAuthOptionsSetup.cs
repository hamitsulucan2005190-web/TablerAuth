using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// OAuth tabanlı bir handler'ın ayarlarını appsettings yerine veritabanı
/// kaydından doldurur. Gizli değer yine secret store'dan gelir; DB yalnızca
/// hangi anahtarın okunacağını söyler.
/// </summary>
internal abstract class DynamicOAuthOptionsSetup<TOptions> : IConfigureNamedOptions<TOptions>
    where TOptions : OAuthOptions
{
    private readonly DynamicSchemeStore _store;
    private readonly IdentityProviderKind _kind;

    protected DynamicOAuthOptionsSetup(DynamicSchemeStore store, IdentityProviderKind kind)
    {
        _store = store;
        _kind = kind;
    }

    public void Configure(string? name, TOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        // Liste her zaman DynamicAuthenticationSchemeProvider tarafından önceden
        // yüklenir; burada async okuma yapılamayacağı için hazır anlık görüntü kullanılır.
        if (!_store.Current.Providers.TryGetValue(name, out var provider) || provider.Kind != _kind)
        {
            return;
        }

        options.ClientId = provider.ClientId;
        options.ClientSecret = provider.ClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.CallbackPath = provider.CallbackPath;
        options.SaveTokens = false;

        options.Scope.Clear();
        foreach (var scope in provider.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.Events.OnRemoteFailure = ExternalProviderEventDefaults.HandleRemoteFailure;
        options.Events.OnRedirectToAuthorizationEndpoint = ExternalProviderEventDefaults.ForceAccountPicker;

        ConfigureProvider(provider, options);
    }

    public void Configure(TOptions options)
    {
    }

    /// <summary>Sağlayıcıya özgü ek ayarlar.</summary>
    protected virtual void ConfigureProvider(ResolvedExternalProvider provider, TOptions options)
    {
    }
}
