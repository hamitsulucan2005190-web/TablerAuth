using System.ComponentModel.DataAnnotations;

namespace TablerAuth.Web.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me on this device")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
