using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.WebUtilities;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

internal sealed class DynamicGoogleOptionsSetup : DynamicOAuthOptionsSetup<GoogleOptions>
{
    public DynamicGoogleOptionsSetup(DynamicSchemeStore store)
        : base(store, IdentityProviderKind.Google)
    {
    }

    protected override void ConfigureProvider(ResolvedExternalProvider provider, GoogleOptions options)
    {
        // Google tek oturumu hatırlayıp sessizce giriş yapmasın;
        // kullanıcı her seferinde hangi hesapla gireceğini seçsin.
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(
                QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "select_account"));
            return Task.CompletedTask;
        };
    }
}
