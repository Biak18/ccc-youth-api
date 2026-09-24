namespace CityYouth.Application.Features.Events;

public sealed record EventDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? CoverImageUrl,
    string? Location,
    DateTime StartDate,
    DateTime? EndDate,
    string? RegistrationUrl,
    string? ContactInformation,
    string Status,
    Guid? CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record EventListDto(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string? Location,
    DateTime StartDate,
    DateTime? EndDate,
    string Status);
