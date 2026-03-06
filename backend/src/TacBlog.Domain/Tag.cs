namespace TacBlog.Domain;

public sealed class Tag : IEquatable<Tag>
{
    public TagId Id { get; }
    public TagName Name { get; }
    public Slug Slug { get; }

    private Tag(TagId id, TagName name, Slug slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
    }

    public static Tag Create(TagName name) =>
        new(TagId.NewUnique(), name, Slug.FromTagName(name));

    public bool Equals(Tag? other) =>
        other is not null && Slug == other.Slug;

    public override bool Equals(object? obj) =>
        Equals(obj as Tag);

    public override int GetHashCode() =>
        Slug.GetHashCode();
}
