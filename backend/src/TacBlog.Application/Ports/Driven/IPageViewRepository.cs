using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface IPageViewRepository
{
    Task SaveAsync(PageView pageView, CancellationToken cancellationToken);
    Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken);
}
