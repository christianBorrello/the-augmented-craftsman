namespace TacBlog.Domain;

public sealed class PageView
{
    public Guid Id { get; }
    public Slug PostSlug { get; }
    public VisitorId VisitorId { get; }
    public DateTime ViewedAtUtc { get; }

    private PageView(Guid id, Slug postSlug, VisitorId visitorId, DateTime viewedAtUtc)
    {
        Id = id;
        PostSlug = postSlug;
        VisitorId = visitorId;
        ViewedAtUtc = viewedAtUtc;
    }

    public static PageView Create(Slug postSlug, VisitorId visitorId, DateTime viewedAtUtc) =>
        new(Guid.NewGuid(), postSlug, visitorId, viewedAtUtc);
}
