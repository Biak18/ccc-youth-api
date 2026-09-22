using System.Text;
using System.Text.RegularExpressions;

namespace CityYouth.Domain.Common;

/// <summary>Publishing workflow: draft → published → archived (§9 ARCHITECTURE).</summary>
public static class ContentStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";

    public static bool IsValid(string? value) => value is Draft or Published or Archived;

    public static string RequireValid(string? value) =>
        IsValid(value) ? value! : throw new DomainException("Status must be draft, published or archived.");
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Leader = "Leader";
}

public static partial class Slugs
{
    public static string Generate(string title)
    {
        var lower = title.Trim().ToLowerInvariant().Replace(' ', '-');
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
            if (char.IsLetterOrDigit(ch) || ch == '-')
                sb.Append(ch);
        var slug = DashCollapse().Replace(sb.ToString(), "-").Trim('-');
        return slug.Length == 0 ? Guid.NewGuid().ToString("N")[..8] : slug[..Math.Min(slug.Length, 120)];
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex DashCollapse();
}
