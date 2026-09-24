using CityYouth.Application.Features.Events;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class EventsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] string? filter,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ListEventsQuery(filter, status, search, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventBySlugQuery(slug), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateEventCommand(
                request.Title, request.Slug, request.Description, request.CoverImageUrl,
                request.Location, request.StartDate, request.EndDate,
                request.RegistrationUrl, request.ContactInformation),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateEventCommand(
                id, request.Title, request.Description, request.CoverImageUrl,
                request.Location, request.StartDate, request.EndDate,
                request.RegistrationUrl, request.ContactInformation),
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
            new UpdateEventStatusCommand(id, request.Status),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteEventCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateEventRequest(
    string Title,
    string? Slug,
    string? Description,
    string? CoverImageUrl,
    string? Location,
    DateTime StartDate,
    DateTime? EndDate,
    string? RegistrationUrl,
    string? ContactInformation);

public sealed record UpdateEventRequest(
    string Title,
    string? Description,
    string? CoverImageUrl,
    string? Location,
    DateTime StartDate,
    DateTime? EndDate,
    string? RegistrationUrl,
    string? ContactInformation);

public sealed record UpdateStatusRequest(string Status);
