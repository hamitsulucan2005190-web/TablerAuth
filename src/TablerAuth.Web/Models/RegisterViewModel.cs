using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TablerAuth.Application.Auth;

namespace TablerAuth.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre tekrarı")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Şartları kabul etmelisiniz.")]
    [Display(Name = "Şartları ve gizlilik politikasını kabul ediyorum")]
    public bool AcceptTerms { get; set; }

    [ValidateNever]
    public IReadOnlyList<ExternalProvider> ExternalProviders { get; set; } = [];
}
