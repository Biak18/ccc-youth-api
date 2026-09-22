using CityYouth.Application.Features.Activities.GetActivityById;
using CityYouth.Application.Features.Admin.GetDashboardStats;
using CityYouth.Application.Features.Announcements.GetAnnouncementById;
using CityYouth.Application.Features.Events.GetEventById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

/// <summary>Dashboard-only endpoints (any signed-in leader).</summary>
[Route("api/admin")]
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
public class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct) =>
        Ok(await sender.Send(new GetDashboardStatsQuery(), ct));

    [HttpGet("activities/{id:guid}")]
    public async Task<IActionResult> ActivityById(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetActivityByIdQuery(id), ct));

    [HttpGet("events/{id:guid}")]
    public async Task<IActionResult> EventById(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetEventByIdQuery(id), ct));

    [HttpGet("announcements/{id:guid}")]
    public async Task<IActionResult> AnnouncementById(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetAnnouncementByIdQuery(id), ct));
}
