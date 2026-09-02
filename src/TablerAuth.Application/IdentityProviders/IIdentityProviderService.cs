namespace TablerAuth.Application.IdentityProviders;

public interface IIdentityProviderService
{
    Task<IReadOnlyList<IdentityProviderListItem>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Form için tek kaydı getirir. Kayıt yoksa null.</summary>
    Task<IdentityProviderInput?> FindAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Kurulu handler'ı olan/olmayan bütün sağlayıcı türleri.</summary>
    IReadOnlyList<IdentityProviderKindOption> ListKinds();

    /// <summary>Id boşsa yeni kayıt, doluysa güncelleme.</summary>
    Task<IdentityProviderSaveResult> SaveAsync(
        IdentityProviderInput input,
        CancellationToken cancellationToken = default);

    /// <summary>Sağlayıcıyı açar/kapatır. Kayıt yoksa false döner.</summary>
    Task<bool> SetEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
