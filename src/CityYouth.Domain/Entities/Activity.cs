using CityYouth.Domain.Common;
using CityYouth.Domain.Exceptions;

namespace CityYouth.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly ActivityDate { get; set; }

    public string? Location { get; set; }

    public string? CoverImageUrl { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public string? Category { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Media> Media { get; set; } = new List<Media>();

    public void Publish()
    {
        if (Status == ContentStatus.Archived)
        {
            throw new DomainException("Archived activities cannot be republished. Create a new activity instead.");
        }

        Status = ContentStatus.Published;
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
