using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfPageViewRepository(TacBlogDbContext context) : IPageViewRepository
{
    public async Task SaveAsync(PageView pageView, CancellationToken cancellationToken)
    {
        context.PageViews.Add(pageView);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.PageViews.CountAsync(v => v.PostSlug == slug, cancellationToken);
}
