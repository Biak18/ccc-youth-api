using CityYouth.Domain.Common;

namespace CityYouth.Domain.Entities;

public sealed class YouthEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? RegistrationUrl { get; set; }
    public string? ContactInformation { get; set; }
    public string Status { get; set; } = ContentStatuses.Draft;
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static YouthEvent Create(string title, DateTimeOffset start, string? createdBy) => new()
    {
        Id = Guid.NewGuid(),
        Title = title.Trim(),
        Slug = Slugs.Generate(title),
        StartDate = start,
        Status = ContentStatuses.Draft,
        CreatedBy = createdBy,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    public void SetStatus(string status) => Status = ContentStatuses.RequireValid(status);

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
