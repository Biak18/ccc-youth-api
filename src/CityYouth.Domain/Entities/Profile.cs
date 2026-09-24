using CityYouth.Domain.Common;

namespace CityYouth.Domain.Entities;

public class Profile
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.Leader;

    public DateTime CreatedAt { get; set; }
}
