namespace TacBlog.Domain;

public readonly record struct VisitorId
{
    private readonly Guid _value;

    public VisitorId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("VisitorId cannot be empty.", nameof(value));

        _value = value;
    }

    public Guid Value => _value;

    public override string ToString() => _value.ToString();
}
