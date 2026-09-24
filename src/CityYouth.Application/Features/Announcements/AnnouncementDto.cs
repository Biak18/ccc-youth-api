namespace CityYouth.Application.Features.Announcements;

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Slug,
    string? Content,
    string? CoverImageUrl,
    DateTime? PublishedAt,
    bool IsPinned,
    string Status,
    Guid? CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt);
