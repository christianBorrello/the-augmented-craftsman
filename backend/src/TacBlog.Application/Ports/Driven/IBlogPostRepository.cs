using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface IBlogPostRepository
{
    Task SaveAsync(BlogPost post, CancellationToken cancellationToken);
    Task<BlogPost?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<BlogPost?> FindByIdAsync(PostId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BlogPost>> FindAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task DeleteAsync(PostId id, CancellationToken cancellationToken);
}
