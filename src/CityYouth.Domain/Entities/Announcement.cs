using CityYouth.Domain.Common;

namespace CityYouth.Domain.Entities;

public sealed class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public bool IsPinned { get; set; }
    public string Status { get; set; } = ContentStatuses.Draft;
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static Announcement Create(string title, string? createdBy) => new()
    {
        Id = Guid.NewGuid(),
        Title = title.Trim(),
        Slug = Slugs.Generate(title),
        Status = ContentStatuses.Draft,
        CreatedBy = createdBy,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>Domain rule: publishing stamps the publish date (once).</summary>
    public void SetStatus(string status)
    {
        Status = ContentStatuses.RequireValid(status);
        if (status == ContentStatuses.Published && PublishedAt is null)
            PublishedAt = DateTimeOffset.UtcNow;
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
