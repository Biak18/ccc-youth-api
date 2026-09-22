using CityYouth.Application.Abstractions;
using CityYouth.Domain.Common;

namespace CityYouth.Application.Common;

public static class SlugHelper
{
    private sealed record IdRow(Guid Id);

    public static async Task<string> UniqueAsync(
        ISupabaseGateway db, string table, string baseSlug, Guid? exclude = null,
        CancellationToken ct = default)
    {
        var slug = baseSlug;
        for (var i = 2; ; i++)
        {
            var existing = await db.SingleAsync<IdRow>(
                table, $"select=id&slug=eq.{Uri.EscapeDataString(slug)}", ct: ct);
            if (existing is null
                || (exclude is { } id && existing.Id == id))
                return slug;
            slug = $"{baseSlug}-{i}";
        }
    }

    public static string FromTitle(string title, string? explicitSlug) =>
        string.IsNullOrWhiteSpace(explicitSlug) ? Slugs.Generate(title) : explicitSlug.Trim();
}
