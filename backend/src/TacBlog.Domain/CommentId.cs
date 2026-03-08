namespace TacBlog.Domain;

public readonly record struct CommentId
{
    private readonly Guid _value;

    public CommentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CommentId cannot be empty.", nameof(value));

        _value = value;
    }

    public Guid Value => _value;

    public static CommentId NewUnique() => new(Guid.NewGuid());

    public override string ToString() => _value.ToString();
}
