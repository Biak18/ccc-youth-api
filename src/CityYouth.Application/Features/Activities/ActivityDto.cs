namespace CityYouth.Application.Features.Activities;

public sealed record ActivityDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    DateOnly ActivityDate,
    string? Location,
    string? CoverImageUrl,
    string Status,
    string? Category,
    Guid? CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MediaCount);

public sealed record ActivityListDto(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageUrl,
    DateOnly ActivityDate,
    string? Category,
    string Status);
