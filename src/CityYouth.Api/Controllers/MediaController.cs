using CityYouth.Application.Features.Media.ListMedia;
using CityYouth.Application.Features.Media.ListMedia;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityYouth.Api.Controllers;

[Route("api/media")]
[ApiController]
public class MediaController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] Guid? activityId = null, [FromQuery] string? type = null,
        [FromQuery] int limit = 100, CancellationToken ct = default) =>
        Ok(await sender.Send(new ListMediaQuery(activityId, type, limit), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        CreateMediaCommand command, CancellationToken ct)
    {
        var item = await sender.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetMediaQuery(id), ct));

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id, UpdateMediaCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteMediaCommand(id), ct);
        return NoContent();
    }
}
