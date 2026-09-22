using CityYouth.Application.Features.Activities.CreateActivity;
using CityYouth.Application.Features.Activities.GetActivity;
using CityYouth.Application.Features.Activities.ListActivities;
using CityYouth.Application.Features.Activities.UpdateActivity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityYouth.Api.Controllers;

[Route("api/activities")]
[ApiController]
public class ActivitiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] string status = "published", [FromQuery] int? year = null,
        [FromQuery] string? category = null, [FromQuery] int limit = 24,
        [FromQuery] int offset = 0, CancellationToken ct = default) =>
        Ok(await sender.Send(
            new ListActivitiesQuery(status, year, category, limit, offset), ct));

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> BySlug(string slug, CancellationToken ct) =>
        Ok(await sender.Send(new GetActivityQuery(slug), ct));

    [HttpGet("{id:guid}/media")]
    [AllowAnonymous]
    public async Task<IActionResult> Media(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetActivityMediaQuery(id), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        CreateActivityCommand command, CancellationToken ct)
    {
        var item = await sender.Send(command, ct);
        return CreatedAtAction(nameof(BySlug), new { slug = item.Slug }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id, UpdateActivityCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command with { Id = id }, ct));

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> SetStatus(
        Guid id, StatusRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new SetActivityStatusCommand(id, body.Status), ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteActivityCommand(id), ct);
        return NoContent();
    }
}
