namespace TacBlog.Domain;

public sealed class BlogPost
{
    public PostId Id { get; }
    public Title Title { get; }
    public Slug Slug { get; }
    public PostContent Content { get; }
    public PostStatus Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }

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
}
