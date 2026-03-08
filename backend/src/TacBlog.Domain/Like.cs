namespace TacBlog.Domain;

public sealed class Like
{
    public Slug PostSlug { get; }
    public VisitorId VisitorId { get; }
    public DateTime CreatedAtUtc { get; }

    private Like(Slug postSlug, VisitorId visitorId, DateTime createdAtUtc)
    {
        PostSlug = postSlug;
        VisitorId = visitorId;
        CreatedAtUtc = createdAtUtc;
    }

    public static Like Create(Slug postSlug, VisitorId visitorId, DateTime createdAtUtc) =>
        new(postSlug, visitorId, createdAtUtc);
}
