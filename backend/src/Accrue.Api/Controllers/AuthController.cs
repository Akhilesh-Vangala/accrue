using Accrue.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Accrue.Api.Controllers;

/// <summary>Login, refresh and password endpoints come from MapIdentityApi under /api/auth; these complement them.</summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me()
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await users.GetRolesAsync(user);
        return new MeDto(user.Email!, user.DisplayName, [.. roles]);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }
}
