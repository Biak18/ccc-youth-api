namespace CityYouth.Domain.Common;

// API labels are lowercase member names (draft, published, archived),
// matching the Supabase values. Persisted as text via an EF value converter.
public enum ContentStatus
{
    Draft,

    Published,

    Archived,
}

public static class ContentStatusExtensions
{
    public static string ToApiString(this ContentStatus status) =>
        status switch
        {
            ContentStatus.Draft => "draft",
            ContentStatus.Published => "published",
            ContentStatus.Archived => "archived",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

    public static bool TryParseApiString(string? value, out ContentStatus status)
    {
        status = ContentStatus.Draft;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var candidate in Enum.GetValues<ContentStatus>())
        {
            if (string.Equals(candidate.ToApiString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                status = candidate;
                return true;
            }
        }

        return false;
    }

    public static ContentStatus ParseApiString(string value) =>
        TryParseApiString(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown content status '{value}'.");
}
