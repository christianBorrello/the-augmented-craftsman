namespace TacBlog.Domain;

public readonly record struct PostContent
{
    private readonly string _value;

    public PostContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PostContent cannot be empty or whitespace.", nameof(value));

        _value = value;
    }

    public string Value => _value;

    public override string ToString() => _value;
}
