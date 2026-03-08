using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface ICommentRepository
{
    Task SaveAsync(Comment comment, CancellationToken cancellationToken);
    Task<IReadOnlyList<Comment>> FindBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<Comment?> FindByIdAsync(CommentId commentId, CancellationToken cancellationToken);
    Task DeleteAsync(CommentId commentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Comment>> FindAllAsync(CancellationToken cancellationToken);
}
