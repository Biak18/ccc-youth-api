using CityYouth.Domain.Common;
using CityYouth.Domain.Exceptions;

namespace CityYouth.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? Location { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? RegistrationUrl { get; set; }

    public string? ContactInformation { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Publish()
    {
        if (Status == ContentStatus.Archived)
        {
            throw new DomainException("Archived events cannot be republished. Create a new event instead.");
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
