using System.Text.RegularExpressions;

namespace CityYouth.Application.Common;

public static partial class SlugHelper
{
    public static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    public static string EnsureUnique(string baseSlug, Func<string, bool> exists)
    {
        if (!exists(baseSlug))
        {
            return baseSlug;
        }

        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!exists(candidate))
            {
                return candidate;
            }
        }

        return $"{baseSlug}-{Guid.NewGuid():N}";
    }
}
