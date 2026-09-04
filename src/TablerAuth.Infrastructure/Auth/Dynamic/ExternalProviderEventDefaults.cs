using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// Bütün OAuth sağlayıcılarında aynı olan davranışlar. Her binding kendi
/// options kurulumunda bunları çağırır, böylece kopyala-yapıştır olmaz.
/// </summary>
internal static class ExternalProviderEventDefaults
{
    /// <summary>
    /// Callback yolu herkese açık; state'i olmayan istek 500 yerine giriş
    /// sayfasına döner. Hatanın ayrıntısı kullanıcıya sızdırılmaz.
    /// </summary>
    public static Task HandleRemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();

        var target = string.IsNullOrWhiteSpace(context.Properties?.RedirectUri)
            ? "/Account/Login"
            : context.Properties!.RedirectUri!;

        context.Response.Redirect(
            QueryHelpers.AddQueryString(target, "remoteError", "external_failure"));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Google / GitHub / LinkedIn tarayıcıdaki tek oturumu hatırlayıp sessizce
    /// giriş yapmasın; kullanıcı her seferinde hangi hesabı kullanacağını seçsin.
    /// </summary>
    public static Task ForceAccountPicker(RedirectContext<OAuthOptions> context)
    {
        context.Response.Redirect(
            QueryHelpers.AddQueryString(context.RedirectUri, "prompt", "select_account"));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Generic OIDC için aynı hesap seçici. OAuth olayından ayrıdır çünkü
    /// OpenID Connect <c>prompt</c> değerini protokol mesajına yazar.
    /// </summary>
    public static Task ForceOidcAccountPicker(RedirectContext context)
    {
        context.ProtocolMessage.Prompt = "select_account";
        return Task.CompletedTask;
    }
}
