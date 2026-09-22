using CityYouth.Application.Features.Auth.GetMe;
using CityYouth.Application.Features.Auth.Login;
using CityYouth.Application.Features.Auth.Refresh;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[Route("api/auth")]
[ApiController]
[EnableRateLimiting("api")]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Exchange email + password for a Supabase access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Rotate an expired session. Returns a new access token AND a new
    /// refresh token — replace both stored values.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Caller id/email from the token plus role from profiles.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Ok(await sender.Send(new GetMeQuery(), ct));
}
