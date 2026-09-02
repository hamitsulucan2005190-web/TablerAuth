using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.Auth;
using TablerAuth.Web.Models;

namespace TablerAuth.Web.Controllers;

public class HomeController : Controller
{
    private readonly IAuthService _authService;

    public HomeController(IAuthService authService)
    {
        _authService = authService;
    }

    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Kontrol paneli";
        ViewData["Pretitle"] = "Özet";
        ViewData["Description"] = "Kısa bir karşılama. Hesap ve giriş yöntemleri Profil’dedir.";

        var user = await _authService.GetSignedInUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        return View(user);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Gizlilik politikası";
        ViewData["Pretitle"] = "Yasal";
        ViewData["Description"] = "Giriş yaptığınızda bu uygulamanın sakladığı bilgiler.";
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
