using System.ComponentModel.DataAnnotations;

namespace TablerAuth.Web.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta adresi")]
    public string Email { get; set; } = string.Empty;
}
