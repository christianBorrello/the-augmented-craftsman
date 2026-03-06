namespace TacBlog.Domain;

public readonly record struct PostId
{
    private readonly Guid _value;

    public PostId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PostId cannot be empty.", nameof(value));

        _value = value;
    }

    public Guid Value => _value;

    public static PostId NewUnique() => new(Guid.NewGuid());

    public override string ToString() => _value.ToString();
}
