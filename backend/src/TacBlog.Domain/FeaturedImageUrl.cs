namespace TacBlog.Domain;

public readonly record struct FeaturedImageUrl
{
    private readonly string _value;

    public FeaturedImageUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Featured image URL cannot be empty or whitespace.", nameof(value));

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException("Featured image URL must be a valid absolute URL.", nameof(value));

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Featured image URL must use HTTPS.", nameof(value));

        if (string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Featured image URL must be a valid absolute URL.", nameof(value));

        _value = uri.AbsoluteUri;
    }

    public string Value => _value;

    public override string ToString() => _value;
}
