namespace CityYouth.Domain.Common;

public static class MediaTypes
{
    public const string Image = "image";
    public const string Video = "video";

    public static bool IsValid(string? value) =>
        value is Image or Video;
}

public static class MediaSources
{
    public const string Storage = "storage";
    public const string Youtube = "youtube";
    public const string External = "external";

    public static bool IsValid(string? value) =>
        value is Storage or Youtube or External;
}
