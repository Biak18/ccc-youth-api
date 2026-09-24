using CityYouth.Application.Features.Media;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class MediaController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] Guid? activityId,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ListMediaQuery(activityId, type, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMediaByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateMediaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateMediaCommand(
                request.ActivityId, request.Type, request.Source, request.Url,
                request.ThumbnailUrl, request.Title, request.Description, request.SortOrder),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMediaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateMediaCommand(id, request.ThumbnailUrl, request.Title, request.Description, request.SortOrder),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteMediaCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateMediaRequest(
    Guid? ActivityId,
    string Type,
    string Source,
    string Url,
    string? ThumbnailUrl,
    string? Title,
    string? Description,
    int SortOrder = 0);

public sealed record UpdateMediaRequest(
    string? ThumbnailUrl,
    string? Title,
    string? Description,
    int SortOrder = 0);
