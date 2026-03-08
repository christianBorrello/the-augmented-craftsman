using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface ILikeRepository
{
    Task SaveAsync(Like like, CancellationToken cancellationToken);
    Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Slug slug, VisitorId visitorId, CancellationToken cancellationToken);
}
