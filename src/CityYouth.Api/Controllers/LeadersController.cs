using CityYouth.Application.Features.Leaders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class LeadersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListLeadersQuery(IncludeHidden: false), cancellationToken);
        return Ok(result);
    }

    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> ListAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListLeadersQuery(IncludeHidden: true), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLeaderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLeaderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateLeaderCommand(
                request.UserId, request.Name, request.RoleTitle, request.PhotoUrl,
                request.Bio, request.SortOrder, request.IsVisible),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLeaderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateLeaderCommand(
                id, request.UserId, request.Name, request.RoleTitle, request.PhotoUrl,
                request.Bio, request.SortOrder, request.IsVisible),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLeaderCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateLeaderRequest(
    Guid? UserId,
    string Name,
    string? RoleTitle,
    string? PhotoUrl,
    string? Bio,
    int SortOrder = 0,
    bool IsVisible = true);

public sealed record UpdateLeaderRequest(
    Guid? UserId,
    string Name,
    string? RoleTitle,
    string? PhotoUrl,
    string? Bio,
    int SortOrder,
    bool IsVisible);
