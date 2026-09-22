using CityYouth.Application.Features.Announcements.ListAnnouncements;
using CityYouth.Application.Features.Announcements.ManageAnnouncements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityYouth.Api.Controllers;

[Route("api/announcements")]
[ApiController]
public class AnnouncementsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] int limit = 20, CancellationToken ct = default) =>
        Ok(await sender.Send(new ListAnnouncementsQuery(limit), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        CreateAnnouncementCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id, UpdateAnnouncementCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command with { Id = id }, ct));

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> SetStatus(
        Guid id, StatusRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new SetAnnouncementStatusCommand(id, body.Status), ct));

    [HttpPatch("{id:guid}/pin")]
    [Authorize]
    public async Task<IActionResult> SetPin(
        Guid id, PinRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new SetAnnouncementPinCommand(id, body.IsPinned), ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteAnnouncementCommand(id), ct);
        return NoContent();
    }
}
