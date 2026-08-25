using Microsoft.AspNetCore.Identity;

namespace TablerAuth.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
