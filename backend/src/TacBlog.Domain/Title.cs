namespace TacBlog.Domain;

public readonly record struct Title
{
    private const int MaxLength = 200;
    private readonly string _value;

    public Title(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Title cannot be empty or whitespace.", nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"Title cannot exceed {MaxLength} characters.", nameof(value));

        _value = trimmed;
    }

    public string Value => _value;

    public override string ToString() => _value;
}
