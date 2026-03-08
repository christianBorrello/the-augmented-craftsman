using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfCommentRepository(TacBlogDbContext context) : ICommentRepository
{
    public async Task SaveAsync(Comment comment, CancellationToken cancellationToken)
    {
        context.Comments.Add(comment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Comment>> FindBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Comments
            .Where(c => c.PostSlug == slug)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Comments.CountAsync(c => c.PostSlug == slug, cancellationToken);

    public async Task<Comment?> FindByIdAsync(CommentId commentId, CancellationToken cancellationToken) =>
        await context.Comments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

    public async Task DeleteAsync(CommentId commentId, CancellationToken cancellationToken)
    {
        var comment = await context.Comments.FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        if (comment is not null)
        {
            context.Comments.Remove(comment);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Comment>> FindAllAsync(CancellationToken cancellationToken) =>
        await context.Comments
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
