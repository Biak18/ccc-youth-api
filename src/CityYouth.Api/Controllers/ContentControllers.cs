using CityYouth.Application.Features.Categories.GetCategories;
using CityYouth.Application.Features.Settings.GetSettings;
using CityYouth.Application.Features.Settings.UpdateSettings;
using CityYouth.Application.Features.Uploads.UploadImage;
using CityYouth.Application.Features.Users.ManageProfiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityYouth.Api.Controllers;

[Route("api/settings")]
[ApiController]
public class SettingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await sender.Send(new GetSettingsQuery(), ct));

    [HttpPut]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(
        UpdateSettingsCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));
}

[Route("api/categories")]
[ApiController]
public class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await sender.Send(new GetCategoriesQuery(), ct));
}

[Route("api/uploads")]
[ApiController]
public class UploadsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] string bucket, [FromForm] string folder, IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file received." });
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var url = await sender.Send(new UploadImageCommand(
            bucket, folder, file.FileName, ms.ToArray(),
            file.ContentType ?? "application/octet-stream"), ct);
        return Ok(new { url });
    }
}

[Route("api/users")]
[ApiController]
[Authorize(Policy = "Admin")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<IActionResult> Profiles(CancellationToken ct) =>
        Ok(await sender.Send(new ListProfilesQuery(), ct));

    [HttpPut("profiles/{id:guid}/role")]
    public async Task<IActionResult> SetRole(
        Guid id, RoleRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new SetProfileRoleCommand(id, body.Role), ct));
}

[Route("api/health")]
[ApiController]
public class HealthController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Health() =>
        Ok(new { status = "ok", time = DateTimeOffset.UtcNow });

    [HttpGet("db")]
    [AllowAnonymous]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        await sender.Send(
            new CityYouth.Application.Features.Activities.ListActivities.ListActivitiesQuery(
                "published", null, null, 1, 0), ct);
        return Ok(new { status = "ok", supabase = "reachable" });
    }
}
