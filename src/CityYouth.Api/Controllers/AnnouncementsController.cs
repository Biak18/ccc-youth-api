using CityYouth.Application.Features.Announcements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class AnnouncementsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ListAnnouncementsQuery(status, search, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAnnouncementByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAnnouncementBySlugQuery(slug), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAnnouncementCommand(
                request.Title, request.Slug, request.Content, request.CoverImageUrl, request.IsPinned),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateAnnouncementCommand(id, request.Title, request.Content, request.CoverImageUrl),
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateAnnouncementStatusCommand(id, request.Status),
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/pin")]
    [Authorize]
    public async Task<IActionResult> UpdatePin(
        Guid id,
        [FromBody] UpdatePinRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateAnnouncementPinCommand(id, request.IsPinned),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAnnouncementCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateAnnouncementRequest(
    string Title,
    string? Slug,
    string? Content,
    string? CoverImageUrl,
    bool IsPinned = false);

public sealed record UpdateAnnouncementRequest(
    string Title,
    string? Content,
    string? CoverImageUrl);

public sealed record UpdatePinRequest(bool IsPinned);
