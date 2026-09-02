using AspNet.Security.OAuth.GitHub;
using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

internal sealed class GoogleProviderBinding : IExternalProviderBinding
{
    public IdentityProviderKind Kind => IdentityProviderKind.Google;

    public Type HandlerType => typeof(GoogleHandler);

    public string DefaultCallbackPath => "/signin-google";

    public string DefaultScopes => "openid profile email";

    public bool RequiresAuthority => false;
}

internal sealed class GitHubProviderBinding : IExternalProviderBinding
{
    public IdentityProviderKind Kind => IdentityProviderKind.GitHub;

    public Type HandlerType => typeof(GitHubAuthenticationHandler);

    public string DefaultCallbackPath => "/signin-github";

    // GitHub e-postayı gizli tutabiliyor; "user:email" olmadan handler
    // /user/emails ucuna gitmez ve e-posta claim'i gelmez.
    public string DefaultScopes => "read:user user:email";

    public bool RequiresAuthority => false;
}

internal sealed class LinkedInProviderBinding : IExternalProviderBinding
{
    public IdentityProviderKind Kind => IdentityProviderKind.LinkedIn;

    public Type HandlerType => typeof(LinkedInAuthenticationHandler);

    public string DefaultCallbackPath => "/signin-linkedin";

    public string DefaultScopes => "openid profile email";

    public bool RequiresAuthority => false;
}

internal sealed class OidcProviderBinding : IExternalProviderBinding
{
    public IdentityProviderKind Kind => IdentityProviderKind.Oidc;

    public Type HandlerType => typeof(OpenIdConnectHandler);

    public string DefaultCallbackPath => "/signin-oidc";

    public string DefaultScopes => "openid profile email";

    public bool RequiresAuthority => true;
}
