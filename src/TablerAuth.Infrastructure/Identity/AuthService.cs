using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TablerAuth.Application.Auth;
using TablerAuth.Application.Email;
using TablerAuth.Application.Tokens;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;
using TablerAuth.Infrastructure.Data;

namespace TablerAuth.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _db;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEmailSender emailSender,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _db = db;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.Name
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Fail(createResult.Errors.Select(error => error.Description).ToArray());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            return AuthResult.Fail(roleResult.Errors.Select(error => error.Description).ToArray());
        }

        await _signInManager.SignInAsync(user, isPersistent: false, SignInMethods.Password);
        return AuthResult.Success();
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is not null)
            {
                await _signInManager.SignInAsync(user, request.RememberMe, SignInMethods.Password);
            }

            return AuthResult.Success();
        }

        if (result.IsLockedOut)
        {
            return AuthResult.Fail("Bu hesap kilitli. Daha sonra tekrar deneyin.");
        }

        return AuthResult.Fail("E-posta veya şifre yanlış.");
    }

    public async Task<TokenAuthResult> ApiLoginAsync(
        ApiLoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return TokenAuthResult.Fail("E-posta veya şifre yanlış.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return TokenAuthResult.Fail("Bu hesap kilitli. Daha sonra tekrar deneyin.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return TokenAuthResult.Fail("E-posta veya şifre yanlış.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        var tokens = await _tokenService.IssueTokensAsync(
            new TokenUser
            {
                Id = user.Id,
                Email = user.Email ?? request.Email,
                DisplayName = user.DisplayName,
                Roles = roles,
                Permissions = RolePermissions.ForRoles(roles)
            },
            ipAddress,
            cancellationToken);

        return TokenAuthResult.Success(tokens);
    }

    public async Task<IReadOnlyList<ExternalProvider>> GetExternalProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();

        return schemes
            .Select(scheme => new ExternalProvider
            {
                Scheme = scheme.Name,
                DisplayName = scheme.DisplayName ?? scheme.Name
            })
            .ToList();
    }

    public async Task<ExternalLoginChallenge?> CreateExternalLoginChallengeAsync(
        string scheme,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var providers = await GetExternalProvidersAsync(cancellationToken);
        if (!providers.Any(provider => provider.Scheme == scheme))
        {
            return null;
        }

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(scheme, redirectUri);

        return new ExternalLoginChallenge
        {
            Scheme = scheme,
            RedirectUri = redirectUri,
            Items = new Dictionary<string, string?>(properties.Items)
        };
    }

    public async Task<AuthResult> ExternalLoginAsync(CancellationToken cancellationToken = default)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return AuthResult.Fail("Dış giriş tamamlanamadı. Lütfen tekrar deneyin.");
        }

        var providerName = info.ProviderDisplayName ?? info.LoginProvider;

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            return AuthResult.Success();
        }

        if (signInResult.IsLockedOut)
        {
            return AuthResult.Fail("Bu hesap kilitli. Daha sonra tekrar deneyin.");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthResult.Fail($"{providerName} e-posta adresi paylaşmadığı için hesap oluşturulamadı.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return AuthResult.Fail(createResult.Errors.Select(error => error.Description).ToArray());
            }

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
            if (!roleResult.Succeeded)
            {
                return AuthResult.Fail(roleResult.Errors.Select(error => error.Description).ToArray());
            }
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            return AuthResult.Fail(addLoginResult.Errors.Select(error => error.Description).ToArray());
        }

        await _signInManager.Context.SignOutAsync(IdentityConstants.ExternalScheme);
        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return AuthResult.Success();
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return _signInManager.SignOutAsync();
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new ForgotPasswordResult();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = request.CreateResetLink(token);
        var emailSent = await _emailSender.SendPasswordResetAsync(
            user.Email ?? request.Email,
            resetUrl,
            cancellationToken);

        return new ForgotPasswordResult
        {
            ResetToken = request.IncludeResetToken ? token : null,
            EmailSent = emailSent
        };
    }

    public async Task<AuthResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthResult.Fail("Bu sıfırlama bağlantısı geçersiz. Yeni bir tane isteyin.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return AuthResult.Fail(result.Errors.Select(error => error.Description).ToArray());
        }

        return AuthResult.Success();
    }

    public async Task<SignedInUserInfo?> GetSignedInUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var displayNames = await ResolveDisplayNamesAsync(cancellationToken);

        var currentScheme = principal.FindFirstValue(ClaimTypes.AuthenticationMethod);
        if (string.IsNullOrWhiteSpace(currentScheme))
        {
            currentScheme = SignInMethods.Password;
        }

        var linked = new List<SignInMethodInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddMethod(linked, seen, currentScheme, displayNames, currentScheme);
        if (hasPassword)
        {
            AddMethod(linked, seen, SignInMethods.Password, displayNames, currentScheme);
        }

        foreach (var login in logins)
        {
            AddMethod(linked, seen, login.LoginProvider, displayNames, currentScheme);
        }

        return new SignedInUserInfo
        {
            Email = user.Email ?? principal.Identity?.Name ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray(),
            Permissions = RolePermissions.ForRoles(roles),
            CurrentSignIn = CreateMethod(currentScheme, displayNames, usedThisSession: true),
            LinkedMethods = linked,
            HasPassword = hasPassword
        };
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var stored = await _db.IdentityProviders
            .AsNoTracking()
            .Select(provider => new { provider.Scheme, provider.DisplayName })
            .ToListAsync(cancellationToken);

        foreach (var row in stored)
        {
            names[row.Scheme] = row.DisplayName;
        }

        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        foreach (var scheme in schemes)
        {
            if (!names.ContainsKey(scheme.Name))
            {
                names[scheme.Name] = scheme.DisplayName ?? scheme.Name;
            }
        }

        return names;
    }

    private static void AddMethod(
        List<SignInMethodInfo> methods,
        HashSet<string> seen,
        string scheme,
        IReadOnlyDictionary<string, string> displayNames,
        string currentScheme)
    {
        if (!seen.Add(scheme))
        {
            return;
        }

        var usedThisSession = string.Equals(scheme, currentScheme, StringComparison.OrdinalIgnoreCase);
        methods.Add(CreateMethod(scheme, displayNames, usedThisSession));
    }

    private static SignInMethodInfo CreateMethod(
        string scheme,
        IReadOnlyDictionary<string, string> displayNames,
        bool usedThisSession)
    {
        var isPassword = string.Equals(scheme, SignInMethods.Password, StringComparison.OrdinalIgnoreCase);

        return new SignInMethodInfo
        {
            Scheme = isPassword ? SignInMethods.Password : scheme,
            DisplayName = isPassword
                ? SignInMethods.PasswordDisplayName
                : displayNames.TryGetValue(scheme, out var displayName) ? displayName : scheme,
            IsExternal = !isPassword,
            UsedThisSession = usedThisSession
        };
    }
}
