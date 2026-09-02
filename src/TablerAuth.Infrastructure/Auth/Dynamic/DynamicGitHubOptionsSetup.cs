using AspNet.Security.OAuth.GitHub;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

internal sealed class DynamicGitHubOptionsSetup : DynamicOAuthOptionsSetup<GitHubAuthenticationOptions>
{
    public DynamicGitHubOptionsSetup(DynamicSchemeStore store)
        : base(store, IdentityProviderKind.GitHub)
    {
    }
}
