using System.ComponentModel.DataAnnotations;
using TablerAuth.Application.IdentityProviders;

namespace TablerAuth.Web.Models;

public sealed class NotPastedSecretAttribute : ValidationAttribute
{
    public NotPastedSecretAttribute()
    {
        ErrorMessage = SecretStoreKeyRules.PastedSecretMessage;
    }

    public override bool IsValid(object? value) =>
        !SecretStoreKeyRules.LooksLikePastedSecret(value as string);
}
