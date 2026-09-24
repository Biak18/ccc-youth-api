namespace CityYouth.Application.Features.Leaders;

public sealed record LeaderDto(
    Guid Id,
    Guid? UserId,
    string Name,
    string? RoleTitle,
    string? PhotoUrl,
    string? Bio,
    int SortOrder,
    bool IsVisible);
