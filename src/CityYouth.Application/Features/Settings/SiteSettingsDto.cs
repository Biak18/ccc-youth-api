namespace CityYouth.Application.Features.Settings;

public sealed record SiteSettingsDto(
    string? ChurchName,
    string? YouthName,
    string? LogoUrl,
    string? HeroImageUrl,
    string? Tagline,
    string? Description,
    string? Address,
    string? Phone,
    string? Email,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? InstagramUrl,
    string[] HeroImages);
