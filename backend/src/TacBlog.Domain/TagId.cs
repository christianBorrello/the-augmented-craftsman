namespace TacBlog.Domain;

public readonly record struct TagId
{
    private readonly Guid _value;

    public TagId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TagId cannot be empty.", nameof(value));

        _value = value;
    }

    public Guid Value => _value;

    public static TagId NewUnique() => new(Guid.NewGuid());

    public override string ToString() => _value.ToString();
}
