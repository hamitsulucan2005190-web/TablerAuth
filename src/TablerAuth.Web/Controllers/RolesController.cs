using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.Admin;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Web.Controllers;

[Authorize(Policy = AppPolicies.ViewRoles)]
public class RolesController : Controller
{
    private readonly IAdminDirectoryService _adminDirectory;

    public RolesController(IAdminDirectoryService adminDirectory)
    {
        _adminDirectory = adminDirectory;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Roller";
        ViewData["Pretitle"] = "Yönetim";
        ViewData["Description"] = "Admin ve User. Menü görünürlüğü kilit değildir — sunucu izin claim’ini yine kontrol eder.";

        var roles = await _adminDirectory.ListRolesAsync(cancellationToken);
        return View(roles);
    }
}
