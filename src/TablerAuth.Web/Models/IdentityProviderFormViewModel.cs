using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TablerAuth.Application.IdentityProviders;

namespace TablerAuth.Web.Models;

public class IdentityProviderFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "İç ad zorunludur.")]
    [StringLength(64)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "İç ad yalnızca harf, rakam, nokta, alt çizgi ve tire içerebilir.")]
    [Display(Name = "İç ad")]
    public string Scheme { get; set; } = string.Empty;

    [Required(ErrorMessage = "Düğme adı zorunludur.")]
    [StringLength(64)]
    [Display(Name = "Düğme adı")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sağlayıcı türü zorunludur.")]
    [Display(Name = "Sağlayıcı türü")]
    public string Kind { get; set; } = string.Empty;

    [Required(ErrorMessage = "Uygulama kimliği zorunludur.")]
    [StringLength(256)]
    [Display(Name = "Uygulama kimliği")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gizli anahtar adı zorunludur.")]
    [StringLength(256)]
    [NotPastedSecret]
    [Display(Name = "Gizli anahtar adı")]
    public string ClientSecretKey { get; set; } = string.Empty;

    [StringLength(256)]
    [Url(ErrorMessage = "Geçerli bir adres girin.")]
    [Display(Name = "Yetkili sunucu adresi")]
    public string? Authority { get; set; }

    [StringLength(512)]
    [Display(Name = "Kullanıcıdan istenenler")]
    public string? Scopes { get; set; }

    [StringLength(128)]
    [RegularExpression(@"^/[A-Za-z0-9\-._~/]*$", ErrorMessage = "Dönüş yolu / ile başlamalı ve soru işareti içermemelidir.")]
    [Display(Name = "Dönüş yolu")]
    public string? CallbackPath { get; set; }

    [Display(Name = "Giriş sayfasında göster")]
    public bool Enabled { get; set; }

    [Display(Name = "Sıra")]
    [Range(0, 999)]
    public int SortOrder { get; set; }

    [ValidateNever]
    public IReadOnlyList<IdentityProviderKindOption> Kinds { get; set; } = [];

    public bool IsNew => Id is null;

    public IdentityProviderInput ToInput() => new()
    {
        Id = Id,
        Scheme = Scheme,
        DisplayName = DisplayName,
        Kind = Kind,
        ClientId = ClientId,
        ClientSecretKey = ClientSecretKey,
        Authority = Authority,
        Scopes = Scopes,
        CallbackPath = CallbackPath,
        Enabled = Enabled,
        SortOrder = SortOrder
    };

    public static IdentityProviderFormViewModel FromInput(IdentityProviderInput input) => new()
    {
        Id = input.Id,
        Scheme = input.Scheme,
        DisplayName = input.DisplayName,
        Kind = input.Kind,
        ClientId = input.ClientId,
        ClientSecretKey = input.ClientSecretKey,
        Authority = input.Authority,
        Scopes = input.Scopes,
        CallbackPath = input.CallbackPath,
        Enabled = input.Enabled,
        SortOrder = input.SortOrder
    };
}
