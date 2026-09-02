namespace TablerAuth.Application.IdentityProviders;

public static class SecretStoreKeyRules
{
    public const string PastedSecretMessage =
        "Bu, anahtar adı değil gizli değerin kendisi gibi duruyor. " +
        "Gizliyi User Secrets, ortam değişkeni veya Key Vault’a koyun; " +
        "buraya yalnızca anahtar adını yazın — örneğin Authentication:LinkedIn:ClientSecret.";

    /// <summary>
    /// True when the value is probably a pasted client secret rather than a store key
    /// such as Authentication:Google:ClientSecret or GOOGLE_CLIENT_SECRET.
    /// </summary>
    public static bool LooksLikePastedSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var key = value.Trim();
        if (key.Contains(':'))
        {
            return false;
        }

        if (key.Length < 16)
        {
            return false;
        }

        var hasLower = key.Any(char.IsLower);
        var hasDigitOrSymbol = key.Any(character =>
            char.IsDigit(character) || (!char.IsLetterOrDigit(character) && character != '_'));

        return hasLower && hasDigitOrSymbol;
    }
}
