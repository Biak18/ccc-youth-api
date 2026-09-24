namespace CityYouth.Domain.Entities;

public class SiteSetting
{
    public int Id { get; set; } = 1;

    public string? ChurchName { get; set; }

    public string? YouthName { get; set; }

    public string? LogoUrl { get; set; }

    public string? HeroImageUrl { get; set; }

    public string? Tagline { get; set; }

    public string? Description { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? FacebookUrl { get; set; }

    public string? YoutubeUrl { get; set; }

    public string? InstagramUrl { get; set; }

    public string[] HeroImages { get; set; } = [];
}
