using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfLikeRepository(TacBlogDbContext context) : ILikeRepository
{
    public async Task SaveAsync(Like like, CancellationToken cancellationToken)
    {
        context.Likes.Add(like);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Likes.CountAsync(l => l.PostSlug == slug, cancellationToken);

    public async Task<bool> ExistsAsync(Slug slug, VisitorId visitorId, CancellationToken cancellationToken) =>
        await context.Likes.AnyAsync(
            l => l.PostSlug == slug && l.VisitorId == visitorId,
            cancellationToken);
}
