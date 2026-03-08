namespace TacBlog.Domain;

public readonly record struct CommentText
{
    private const int MaxLength = 2000;
    private readonly string _value;

    public CommentText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Comment text cannot be empty or whitespace.", nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"Comment text cannot exceed {MaxLength} characters.", nameof(value));

        _value = trimmed;
    }

    public string Value => _value;

    public override string ToString() => _value;
}
