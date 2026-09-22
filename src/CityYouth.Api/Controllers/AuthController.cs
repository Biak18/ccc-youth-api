using CityYouth.Application.Features.Auth.GetMe;
using CityYouth.Application.Features.Auth.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityYouth.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Exchange email + password for a Supabase access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
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
