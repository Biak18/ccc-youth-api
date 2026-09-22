using CityYouth.Domain.Common;
using CityYouth.Infrastructure.Supabase;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CityYouth.Api.Middleware;

/// <summary>Centralized error mapping (§21): exceptions → HTTP status codes.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title, detail, errors) = Map(ex);
        if (status == 500)
            logger.LogError(ex, "Unhandled exception.");

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(
            new { title, detail, status, errors }, ct);
        return true;
    }

    private static (int Status, string Title, string? Detail, object? Errors) Map(Exception ex) =>
        ex switch
        {
            ValidationException vex => (400, "Validation failed.", null,
                vex.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })),
            NotFoundException => (404, "Not found.", ex.Message, null),
            ForbiddenException => (403, "Forbidden.", ex.Message, null),
            SupabaseException sex => (sex.StatusCode is >= 400 and < 500 ? sex.StatusCode : 502,
                "Supabase error.", sex.Message, null),
            SupabaseUnreachableException => (502, "Upstream unavailable.",
                "Supabase is unreachable from this network. Check VPN/proxy rules.", null),
            _ => (500, "Internal server error.", "An unexpected error occurred.", null),
        };
}
