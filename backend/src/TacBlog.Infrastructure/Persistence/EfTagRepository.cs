using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfTagRepository(TacBlogDbContext context) : ITagRepository
{
    public async Task<Tag?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Tags.SingleOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    public async Task<Tag?> FindByIdAsync(TagId id, CancellationToken cancellationToken) =>
        await context.Tags.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        await context.Tags.AnyAsync(t => t.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<TagWithPostCount>> GetAllWithPostCountsAsync(CancellationToken cancellationToken)
    {
        var tags = await context.Tags.ToListAsync(cancellationToken);

        var postCounts = await context.Set<Dictionary<string, object>>("post_tags")
            .GroupBy(pt => pt["tag_id"])
            .Select(g => new { TagId = (Guid)g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countByTagId = postCounts.ToDictionary(x => x.TagId, x => x.Count);

        return tags
            .Select(tag => new TagWithPostCount(tag, countByTagId.GetValueOrDefault(tag.Id.Value)))
            .ToList()
            .AsReadOnly();
    }

    public async Task SaveAsync(Tag tag, CancellationToken cancellationToken)
    {
        var entry = context.Entry(tag);

        if (entry.State == EntityState.Detached)
            context.Tags.Add(tag);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TagId id, CancellationToken cancellationToken)
    {
        var tag = await context.Tags.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag is not null)
        {
            context.Tags.Remove(tag);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<TagWithPostCount>> GetPublicTagsWithPostCountsAsync(CancellationToken cancellationToken)
    {
        var tags = await context.Tags.ToListAsync(cancellationToken);

        var publishedPostCounts = await context.Posts
            .Where(p => p.Status == PostStatus.Published)
            .SelectMany(p => p.Tags)
            .GroupBy(t => t.Id)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countByTagId = publishedPostCounts.ToDictionary(x => x.TagId, x => x.Count);

        return tags
            .Where(tag => countByTagId.ContainsKey(tag.Id))
            .Select(tag => new TagWithPostCount(tag, countByTagId[tag.Id]))
            .ToList()
            .AsReadOnly();
    }
}
