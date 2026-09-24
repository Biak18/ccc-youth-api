using CityYouth.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CityYouth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public sealed class UploadsController(IStorageService storage) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        // Forward the caller's own access token so Supabase storage RLS
        // (staff-only, owner = uploader) applies. No service-role key needed.
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }

        var accessToken = authorization["Bearer ".Length..].Trim();

        await using var stream = request.File.OpenReadStream();
        var url = await storage.UploadAsync(
            request.Bucket,
            request.File.FileName,
            stream,
            request.File.ContentType,
            accessToken,
            cancellationToken);

        return Ok(new { url, bucket = request.Bucket });
    }

    [HttpGet("buckets")]
    [AllowAnonymous]
    public IActionResult Buckets()
    {
        return Ok(storage.AllowedBuckets);
    }
}

public sealed record UploadRequest(string Bucket, IFormFile File);
