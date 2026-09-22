using CityYouth.Application.Features.Events.ListEvents;
using CityYouth.Application.Features.Events.ManageEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityYouth.Api.Controllers;

[Route("api/events")]
[ApiController]
public class EventsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] string filter = "upcoming", [FromQuery] int limit = 12,
        CancellationToken ct = default) =>
        Ok(await sender.Send(new ListEventsQuery(filter, limit), ct));

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> BySlug(string slug, CancellationToken ct) =>
        Ok(await sender.Send(new GetEventQuery(slug), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        CreateEventCommand command, CancellationToken ct)
    {
        var item = await sender.Send(command, ct);
        return CreatedAtAction(nameof(BySlug), new { slug = item.Slug }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id, UpdateEventCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command with { Id = id }, ct));

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> SetStatus(
        Guid id, StatusRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new SetEventStatusCommand(id, body.Status), ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteEventCommand(id), ct);
        return NoContent();
    }
}
