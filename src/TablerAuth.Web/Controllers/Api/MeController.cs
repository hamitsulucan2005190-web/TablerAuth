using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.Auth;
using TablerAuth.Domain.Identity;

namespace TablerAuth.Web.Controllers.Api;

[ApiController]
[Route("api/me")]
[Authorize(AuthenticationSchemes = AuthSchemes.JwtBearer)]
public class MeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            name = User.FindFirstValue(ClaimTypes.Name),
            roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray(),
            permissions = User.FindAll(AppClaimTypes.Permission).Select(claim => claim.Value).ToArray()
        });
    }
}
