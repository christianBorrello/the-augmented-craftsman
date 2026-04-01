using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Posts;

public sealed record PublishScheduledResult(IReadOnlyList<string> PublishedSlugs);

public sealed class PublishScheduledPosts(IBlogPostRepository repository, IClock clock)
{
    public async Task<PublishScheduledResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var duePosts = await repository.FindScheduledDueAsync(clock.UtcNow, cancellationToken);
        var publishedSlugs = new List<string>();

        foreach (var post in duePosts)
        {
            post.Publish(post.ScheduledAt!.Value);
            await repository.SaveAsync(post, cancellationToken);
            publishedSlugs.Add(post.Slug.Value);
        }

        return new PublishScheduledResult(publishedSlugs);
    }
}
