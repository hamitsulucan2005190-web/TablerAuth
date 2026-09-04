using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// Generic OIDC handler ayarlarını veritabanı kaydından doldurur.
/// OAuthOptions ailesinde olmadığı için <see cref="DynamicOAuthOptionsSetup{TOptions}"/>
/// kullanılamaz; Authority discovery adresi buradan gelir.
/// </summary>
internal sealed class DynamicOidcOptionsSetup : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly DynamicSchemeStore _store;

    public DynamicOidcOptionsSetup(DynamicSchemeStore store)
    {
        _store = store;
    }

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (!_store.Current.Providers.TryGetValue(name, out var provider)
            || provider.Kind != IdentityProviderKind.Oidc)
        {
            return;
        }

        options.Authority = provider.Authority;
        options.ClientId = provider.ClientId;
        options.ClientSecret = provider.ClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.CallbackPath = provider.CallbackPath;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = false;

        options.Scope.Clear();
        foreach (var scope in provider.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.Events.OnRemoteFailure = ExternalProviderEventDefaults.HandleRemoteFailure;
        options.Events.OnRedirectToIdentityProvider = ExternalProviderEventDefaults.ForceOidcAccountPicker;
    }

    public void Configure(OpenIdConnectOptions options)
    {
    }
}
