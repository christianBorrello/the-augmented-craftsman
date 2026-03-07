using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfBlogPostRepository(TacBlogDbContext context) : IBlogPostRepository
{
    public async Task SaveAsync(BlogPost post, CancellationToken cancellationToken)
    {
        var entry = context.Entry(post);

        if (entry.State == EntityState.Detached)
            context.Posts.Add(post);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BlogPost?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Posts.Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    public async Task<BlogPost?> FindByIdAsync(PostId id, CancellationToken cancellationToken) =>
        await context.Posts.Include(p => p.Tags)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BlogPost>> FindAllAsync(CancellationToken cancellationToken) =>
        await context.Posts.Include(p => p.Tags)
            .OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);

    public async Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Posts.AnyAsync(p => p.Slug == slug, cancellationToken);

    public async Task DeleteAsync(PostId id, CancellationToken cancellationToken)
    {
        var post = await context.Posts.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (post is not null)
        {
            context.Posts.Remove(post);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Tag?> FindTagBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Tags.SingleOrDefaultAsync(t => t.Slug == slug, cancellationToken);
}
