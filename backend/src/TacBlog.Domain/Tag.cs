namespace TacBlog.Domain;

public sealed class Tag : IEquatable<Tag>
{
    public TagId Id { get; }
    public TagName Name { get; private set; }
    public Slug Slug { get; private set; }
    public TagColor Color { get; private set; }

    private Tag(TagId id, TagName name, Slug slug, TagColor color)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Color = color;
    }

    public static Tag Create(TagName name) =>
        new(TagId.NewUnique(), name, Slug.FromTagName(name), TagColor.Random());

    public void Rename(TagName newName)
    {
        Name = newName;
        Slug = Slug.FromTagName(newName);
    }

    public bool Equals(Tag? other) =>
        other is not null && Slug == other.Slug;

    public override bool Equals(object? obj) =>
        Equals(obj as Tag);

    public override int GetHashCode() =>
        Slug.GetHashCode();
}
