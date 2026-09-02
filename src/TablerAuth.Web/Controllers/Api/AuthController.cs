using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.Auth;
using TablerAuth.Application.Tokens;
using TablerAuth.Web.Models.Api;

namespace TablerAuth.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(ApiLoginViewModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.ApiLoginAsync(
            new ApiLoginRequest
            {
                Email = model.Email,
                Password = model.Password
            },
            ClientIp(),
            cancellationToken);

        if (!result.Succeeded || result.Tokens is null)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.Tokens);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenViewModel model, CancellationToken cancellationToken)
    {
        var result = await _tokenService.RefreshAsync(model.RefreshToken, ClientIp(), cancellationToken);
        if (!result.Succeeded || result.Tokens is null)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.Tokens);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshTokenViewModel model, CancellationToken cancellationToken)
    {
        await _tokenService.RevokeAsync(model.RefreshToken, ClientIp(), cancellationToken);
        return NoContent();
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
