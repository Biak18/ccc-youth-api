using CityYouth.Domain.Common;
using CityYouth.Domain.Exceptions;

namespace CityYouth.Domain.Entities;

public class Announcement
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string? CoverImageUrl { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool IsPinned { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Publish()
    {
        if (Status == ContentStatus.Archived)
        {
            throw new DomainException("Archived announcements cannot be republished. Create a new announcement instead.");
        }

        Status = ContentStatus.Published;
        PublishedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = ContentStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Draft()
    {
        Status = ContentStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
    }
}
