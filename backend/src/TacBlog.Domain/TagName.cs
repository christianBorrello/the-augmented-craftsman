namespace TacBlog.Domain;

public readonly record struct TagName
{
    private const int MaxLength = 50;
    private readonly string _value;

    public TagName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tag name is required", nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"Tag name must be {MaxLength} characters or fewer", nameof(value));

        _value = trimmed;
    }

    public string Value => _value;

    public override string ToString() => _value;
}
