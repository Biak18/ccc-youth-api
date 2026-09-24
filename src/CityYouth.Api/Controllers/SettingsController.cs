using CityYouth.Application.Features.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class SettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSiteSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSiteSettingsCommand(
                request.ChurchName, request.YouthName, request.LogoUrl, request.HeroImageUrl,
                request.Tagline, request.Description, request.Address, request.Phone, request.Email,
                request.FacebookUrl, request.YoutubeUrl, request.InstagramUrl, request.HeroImages),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record UpdateSettingsRequest(
    string? ChurchName,
    string? YouthName,
    string? LogoUrl,
    string? HeroImageUrl,
    string? Tagline,
    string? Description,
    string? Address,
    string? Phone,
    string? Email,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? InstagramUrl,
    string[]? HeroImages);
