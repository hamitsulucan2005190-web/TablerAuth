using Microsoft.AspNetCore.Authentication;
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
}
