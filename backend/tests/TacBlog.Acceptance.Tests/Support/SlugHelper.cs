using System.Text.RegularExpressions;

namespace TacBlog.Acceptance.Tests.Support;

internal static class SlugHelper
{
    internal static string ToSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = Regex.Replace(slug, @"-{2,}", "-");
        return slug.Trim('-');
    }
}
