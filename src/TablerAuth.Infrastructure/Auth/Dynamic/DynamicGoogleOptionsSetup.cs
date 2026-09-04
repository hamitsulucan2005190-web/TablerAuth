using Microsoft.AspNetCore.Authentication.Google;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

internal sealed class DynamicGoogleOptionsSetup : DynamicOAuthOptionsSetup<GoogleOptions>
{
    public DynamicGoogleOptionsSetup(DynamicSchemeStore store)
        : base(store, IdentityProviderKind.Google)
    {
    }
}
