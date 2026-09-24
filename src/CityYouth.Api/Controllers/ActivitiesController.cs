using CityYouth.Application.Features.Activities;
using CityYouth.Application.Features.Media;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class ActivitiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] int? year,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ListActivitiesQuery(status, year, category, search, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetActivityByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetActivityBySlugQuery(slug), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/media")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListActivityMediaQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateActivityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateActivityCommand(
                request.Title, request.Slug, request.Description, request.ActivityDate,
                request.Location, request.CoverImageUrl, request.Category),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateActivityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateActivityCommand(
                id, request.Title, request.Description, request.ActivityDate,
                request.Location, request.CoverImageUrl, request.Category),
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
            new UpdateActivityStatusCommand(id, request.Status),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteActivityCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateActivityRequest(
    string Title,
    string? Slug,
    string? Description,
    DateOnly ActivityDate,
    string? Location,
    string? CoverImageUrl,
    string? Category);

public sealed record UpdateActivityRequest(
    string Title,
    string? Description,
    DateOnly ActivityDate,
    string? Location,
    string? CoverImageUrl,
    string? Category);
