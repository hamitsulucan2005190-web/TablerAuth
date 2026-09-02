using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TablerAuth.Application.Auth;

namespace TablerAuth.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Bu cihazda beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    [ValidateNever]
    public IReadOnlyList<ExternalProvider> ExternalProviders { get; set; } = [];
}
