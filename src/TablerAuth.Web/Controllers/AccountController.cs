using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.Auth;
using TablerAuth.Web.Models;

namespace TablerAuth.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AccountController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (RedirectIfSignedIn(returnUrl) is { } redirect)
        {
            return redirect;
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        model.ExternalProviders = await _authService.GetExternalProvidersAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (RedirectIfSignedIn(model.ReturnUrl) is { } redirect)
        {
            return redirect;
        }

        model.ExternalProviders = await _authService.GetExternalProvidersAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(
            new LoginRequest
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        if (RedirectIfSignedIn() is { } redirect)
        {
            return redirect;
        }

        var model = new RegisterViewModel
        {
            ExternalProviders = await _authService.GetExternalProvidersAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (RedirectIfSignedIn() is { } redirect)
        {
            return redirect;
        }

        model.ExternalProviders = await _authService.GetExternalProvidersAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(
            new RegisterRequest
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLogin(
        string provider,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var redirectUri = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl })
            ?? Url.Action(nameof(Login), "Account")
            ?? "/";

        var challenge = await _authService.CreateExternalLoginChallengeAsync(provider, redirectUri, cancellationToken);
        if (challenge is null)
        {
            return await LoginWithError($"{provider} ile giriş henüz kullanılamıyor.", returnUrl, cancellationToken);
        }

        var properties = new AuthenticationProperties(
            new Dictionary<string, string?>(challenge.Items))
        {
            RedirectUri = challenge.RedirectUri
        };

        return Challenge(properties, challenge.Scheme);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(
        string? returnUrl = null,
        string? remoteError = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            return await LoginWithError("Dış sağlayıcı bir hata bildirdi. Lütfen tekrar deneyin.", returnUrl, cancellationToken);
        }

        var result = await _authService.ExternalLoginAsync(cancellationToken);
        if (!result.Succeeded)
        {
            return await LoginWithError(result.Errors, returnUrl, cancellationToken);
        }

        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Profil";
        ViewData["Pretitle"] = "Hesap";
        ViewData["Description"] = "Hesabınız ve giriş yapabileceğiniz yöntemler.";

        var user = await _authService.GetSignedInUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(cancellationToken);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Erişim reddedildi";
        ViewData["Pretitle"] = "Yetkilendirme";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (RedirectIfSignedIn() is { } redirect)
        {
            return redirect;
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (RedirectIfSignedIn() is { } redirect)
        {
            return redirect;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ForgotPasswordAsync(
            new ForgotPasswordRequest
            {
                Email = model.Email,
                IncludeResetToken = _environment.IsDevelopment(),
                CreateResetLink = token => BuildResetPasswordUrl(model.Email, token)
            },
            cancellationToken);

        string? developmentResetUrl = null;
        if (!string.IsNullOrWhiteSpace(result.ResetToken))
        {
            developmentResetUrl = BuildResetPasswordUrl(model.Email, result.ResetToken);
        }

        return View("ForgotPasswordSent", new ForgotPasswordSentViewModel
        {
            Email = model.Email,
            DevelopmentResetUrl = developmentResetUrl,
            DevelopmentEmailSent = result.EmailSent
        });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (RedirectIfSignedIn() is { } redirect)
        {
            return redirect;
        }

        token = NormalizeResetToken(token);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            ModelState.AddModelError(string.Empty, "Bu sıfırlama bağlantısı geçersiz. Yeni bir tane isteyin.");
        }

        return View(new ResetPasswordViewModel
        {
            Email = email ?? string.Empty,
            Token = token ?? string.Empty
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (RedirectIfSignedIn() is { } redirect)
        {
            return redirect;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(
            new ResetPasswordRequest
            {
                Email = model.Email,
                Token = NormalizeResetToken(model.Token) ?? model.Token,
                NewPassword = model.Password
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Login), new { reset = 1 });
    }

    private Task<IActionResult> LoginWithError(
        string error,
        string? returnUrl,
        CancellationToken cancellationToken) =>
        LoginWithError(new[] { error }, returnUrl, cancellationToken);

    private async Task<IActionResult> LoginWithError(
        IEnumerable<string> errors,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            ExternalProviders = await _authService.GetExternalProvidersAsync(cancellationToken)
        };

        return View(nameof(Login), model);
    }

    private IActionResult? RedirectIfSignedIn(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        return null;
    }

    private string SafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Home") ?? "/";
    }

    private string BuildResetPasswordUrl(string email, string token)
    {
        var path = Url.Action(nameof(ResetPassword), "Account", values: null, protocol: Request.Scheme)
            ?? $"{Request.Scheme}://{Request.Host}/Account/ResetPassword";

        return $"{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private static string? NormalizeResetToken(string? token)
    {
        return string.IsNullOrWhiteSpace(token) ? token : token.Replace(' ', '+');
    }
}
