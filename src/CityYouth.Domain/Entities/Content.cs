namespace CityYouth.Domain.Entities;

public sealed class MediaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActivityId { get; set; }
    public string Type { get; set; } = "image";
    public string Source { get; set; } = "storage";
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string? UploadedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class YouthLeader
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoleTitle { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Bio { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public sealed class SiteSettings
{
    public int Id { get; set; }
    public string? ChurchName { get; set; }
    public string? YouthName { get; set; }
    public string? LogoUrl { get; set; }
    public string? HeroImageUrl { get; set; }
    public string[] HeroImages { get; set; } = [];
    public string? Tagline { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? InstagramUrl { get; set; }
}

public sealed class Profile
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "leader";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
