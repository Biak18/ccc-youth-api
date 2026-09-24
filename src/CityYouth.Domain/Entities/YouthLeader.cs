namespace CityYouth.Domain.Entities;

public class YouthLeader
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? RoleTitle { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Bio { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;
}
