using System.ComponentModel.DataAnnotations;

namespace TablerAuth.Web.Models.Api;

public class RefreshTokenViewModel
{
    [Required(ErrorMessage = "Yenileme jetonu zorunludur.")]
    public string RefreshToken { get; set; } = string.Empty;
}
