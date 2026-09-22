using CityYouth.Application.Features.Leaders.ListLeaders;
using CityYouth.Application.Features.Leaders.ManageLeaders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[Route("api/leaders")]
[ApiController]
[EnableRateLimiting("api")]
public class LeadersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await sender.Send(new ListLeadersQuery(false), ct));

    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> ListAll(CancellationToken ct) =>
        Ok(await sender.Send(new ListLeadersQuery(true), ct));

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create(
        CreateLeaderCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(
        Guid id, UpdateLeaderCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteLeaderCommand(id), ct);
        return NoContent();
    }
}
