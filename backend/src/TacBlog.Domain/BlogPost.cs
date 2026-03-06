namespace TacBlog.Domain;

public sealed class BlogPost
{
    private readonly List<Tag> _tags = [];

    public PostId Id { get; }
    public Title Title { get; private set; }
    public Slug Slug { get; }
    public PostContent Content { get; private set; }
    public PostStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    private BlogPost(PostId id, Title title, Slug slug, PostContent content, PostStatus status, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        Title = title;
        Slug = slug;
        Content = content;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static BlogPost Create(Title title, PostContent content, DateTime createdAt) =>
        new(PostId.NewUnique(), title, Slug.FromTitle(title), content, PostStatus.Draft, createdAt, createdAt);

    public void UpdateTitle(Title title, DateTime updatedAt)
    {
        Title = title;
        UpdatedAt = updatedAt;
    }

    public void UpdateContent(PostContent content, DateTime updatedAt)
    {
        Content = content;
        UpdatedAt = updatedAt;
    }

    public void Publish(DateTime publishedAt)
    {
        if (Status == PostStatus.Published)
            throw new InvalidOperationException("Post is already published.");

        Status = PostStatus.Published;
        PublishedAt = publishedAt;
        UpdatedAt = publishedAt;
    }

    public void AddTag(Tag tag)
    {
        if (_tags.Contains(tag))
            return;

        _tags.Add(tag);
    }

    public void RemoveTag(Tag tag) =>
        _tags.Remove(tag);
}
