namespace CityYouth.Application.Features.Media;

public sealed record MediaDto(
    Guid Id,
    Guid? ActivityId,
    string Type,
    string Source,
    string Url,
    string? ThumbnailUrl,
    string? Title,
    string? Description,
    int SortOrder,
    Guid? UploadedBy,
    DateTime CreatedAt);
