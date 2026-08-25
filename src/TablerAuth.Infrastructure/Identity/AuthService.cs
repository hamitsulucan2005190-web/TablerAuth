using Microsoft.AspNetCore.Identity;
using TablerAuth.Application.Auth;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

        await _signInManager.SignInAsync(user, isPersistent: false);
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
            return AuthResult.Success();
        }

        if (result.IsLockedOut)
        {
            return AuthResult.Fail("This account is locked. Try again later.");
        }

        return AuthResult.Fail("Invalid email or password.");
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return _signInManager.SignOutAsync();
    }
}
