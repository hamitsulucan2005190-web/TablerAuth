using AspNet.Security.OAuth.LinkedIn;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

internal sealed class DynamicLinkedInOptionsSetup : DynamicOAuthOptionsSetup<LinkedInAuthenticationOptions>
{
    public DynamicLinkedInOptionsSetup(DynamicSchemeStore store)
        : base(store, IdentityProviderKind.LinkedIn)
    {
    }
}
