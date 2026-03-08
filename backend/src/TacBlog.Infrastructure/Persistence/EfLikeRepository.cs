using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfLikeRepository(TacBlogDbContext context) : ILikeRepository
{
    public async Task SaveAsync(Like like, CancellationToken cancellationToken)
    {
        context.Likes.Add(like);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.Entry(like).State = EntityState.Detached;
        }
    }

    public async Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Likes.CountAsync(l => l.PostSlug == slug, cancellationToken);

    public async Task<bool> ExistsAsync(Slug slug, VisitorId visitorId, CancellationToken cancellationToken) =>
        await context.Likes.AnyAsync(
            l => l.PostSlug == slug && l.VisitorId == visitorId,
            cancellationToken);

    public async Task DeleteAsync(Slug slug, VisitorId visitorId, CancellationToken cancellationToken)
    {
        var like = await context.Likes.FirstOrDefaultAsync(
            l => l.PostSlug == slug && l.VisitorId == visitorId,
            cancellationToken);

        if (like is not null)
        {
            context.Likes.Remove(like);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
