using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Auth.Dynamic;

/// <summary>
/// Bir sağlayıcı türünü gerçek bir authentication handler'a bağlar.
/// Türü için binding kurulu olmayan DB satırı scheme olarak kaydedilmez;
/// "DB'ye satır eklemek her zaman yeterli değil" kuralı buradan geliyor.
/// </summary>
internal interface IExternalProviderBinding
{
    IdentityProviderKind Kind { get; }

    Type HandlerType { get; }

    string DefaultCallbackPath { get; }

    string DefaultScopes { get; }

    /// <summary>Generic OIDC gibi Authority zorunlu olan türler için true.</summary>
    bool RequiresAuthority { get; }
}
