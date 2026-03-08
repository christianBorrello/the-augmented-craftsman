namespace TacBlog.Domain;

public sealed class Comment
{
    public CommentId Id { get; }
    public Slug PostSlug { get; }
    public string DisplayName { get; }
    public string? AvatarUrl { get; }
    public AuthProvider Provider { get; }
    public CommentText Text { get; }
    public DateTime CreatedAtUtc { get; }

    private Comment(
        CommentId id,
        Slug postSlug,
        string displayName,
        string? avatarUrl,
        AuthProvider provider,
        CommentText text,
        DateTime createdAtUtc)
    {
        Id = id;
        PostSlug = postSlug;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Provider = provider;
        Text = text;
        CreatedAtUtc = createdAtUtc;
    }

    public static Comment Create(
        Slug postSlug,
        string displayName,
        string? avatarUrl,
        AuthProvider provider,
        CommentText text,
        DateTime createdAtUtc) =>
        new(CommentId.NewUnique(), postSlug, displayName, avatarUrl, provider, text, createdAtUtc);
}
