using CityYouth.Application.Features.Categories;
using CityYouth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class CategoriesController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult List()
    {
        return Ok(CategoryCatalog.All);
    }
}

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", service = "cityyouth-api", time = DateTime.UtcNow });
    }

    [HttpGet("db")]
    [AllowAnonymous]
    public async Task<IActionResult> Db(CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Ok(new { status = "ok", database = "reachable" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "error", database = "unreachable" });
    }
}
