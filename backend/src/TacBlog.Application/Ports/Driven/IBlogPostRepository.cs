using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface IBlogPostRepository
{
    Task SaveAsync(BlogPost post, CancellationToken cancellationToken);
    Task<BlogPost?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken);
}
