using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.Admin;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Web.Controllers;

[Authorize(Policy = AppPolicies.ManageUsers)]
public class UsersController : Controller
{
    private readonly IAdminDirectoryService _adminDirectory;

    public UsersController(IAdminDirectoryService adminDirectory)
    {
        _adminDirectory = adminDirectory;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Kullanıcılar";
        ViewData["Pretitle"] = "Yönetim";
        ViewData["Description"] = "Yönetici sayfalarını kim açabilir. Rol değişikliği yalnızca kenar menüde değil, sunucuda da kontrol edilir.";

        var users = await _adminDirectory.ListUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAdmin(string id, bool isAdmin, CancellationToken cancellationToken)
    {
        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actingUserId))
        {
            return Challenge();
        }

        var result = await _adminDirectory.SetAdminRoleAsync(id, isAdmin, actingUserId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Rol değiştirilemedi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["StatusMessage"] = isAdmin
            ? "Bu hesap artık Kullanıcılar, Roller ve Giriş sağlayıcılarını açabilir."
            : "Admin yetkisi alındı. Hesap yine normal kullanıcı olarak giriş yapabilir.";

        return RedirectToAction(nameof(Index));
    }
}
