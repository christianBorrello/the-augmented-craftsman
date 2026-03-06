using System.Text.RegularExpressions;

namespace TacBlog.Domain;

public readonly partial record struct Slug
{
    private readonly string _value;

    public Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty or whitespace.", nameof(value));

        _value = value;
    }

    public static Slug FromTitle(Title title) =>
        FromText(title.ToString());

    public static Slug FromTagName(TagName tagName) =>
        FromText(tagName.ToString());

    private static Slug FromText(string text)
    {
        var slug = text.ToLowerInvariant();
        slug = SpacesPattern().Replace(slug, "-");
        slug = NonAlphanumericPattern().Replace(slug, "");
        slug = MultipleHyphensPattern().Replace(slug, "-");
        slug = slug.Trim('-');

        return new Slug(slug);
    }

    public string Value => _value;

    public override string ToString() => _value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpacesPattern();

    [GeneratedRegex(@"[^a-z0-9-]")]
    private static partial Regex NonAlphanumericPattern();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensPattern();
}
