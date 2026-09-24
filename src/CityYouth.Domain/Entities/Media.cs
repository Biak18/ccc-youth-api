namespace CityYouth.Domain.Entities;

public class Media
{
    public Guid Id { get; set; }

    public Guid? ActivityId { get; set; }

    public string Type { get; set; } = Common.MediaTypes.Image;

    public string Source { get; set; } = Common.MediaSources.Storage;

    public string Url { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public Guid? UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Activity? Activity { get; set; }
}
