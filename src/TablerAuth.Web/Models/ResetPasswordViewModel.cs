using System.ComponentModel.DataAnnotations;

namespace TablerAuth.Web.Models;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sıfırlama bağlantısı geçersiz.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [Display(Name = "Yeni şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre tekrarı")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
