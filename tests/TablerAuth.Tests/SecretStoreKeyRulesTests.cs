using TablerAuth.Application.IdentityProviders;

namespace TablerAuth.Tests;

public class SecretStoreKeyRulesTests
{
    [Theory]
    [InlineData("Authentication:Google:ClientSecret")]
    [InlineData("Authentication:Oidc:ClientSecret")]
    [InlineData("GOOGLE_CLIENT_SECRET")]
    public void Store_key_names_are_not_treated_as_pasted_secrets(string value)
    {
        Assert.False(SecretStoreKeyRules.LooksLikePastedSecret(value));
    }

    [Fact]
    public void Long_mixed_value_without_colon_looks_like_a_pasted_secret()
    {
        Assert.True(SecretStoreKeyRules.LooksLikePastedSecret("GOCSPX-ab12cd34ef56gh78"));
    }

    [Fact]
    public void Null_or_short_values_are_not_pasted_secrets()
    {
        Assert.False(SecretStoreKeyRules.LooksLikePastedSecret(null));
        Assert.False(SecretStoreKeyRules.LooksLikePastedSecret("short"));
    }
}
