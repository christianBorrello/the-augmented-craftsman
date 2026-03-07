using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record BrowsePublishedPostsResult(IReadOnlyList<BlogPost> Posts);

public sealed class BrowsePublishedPosts(IBlogPostRepository repository)
{
    public async Task<BrowsePublishedPostsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var allPosts = await repository.FindAllAsync(cancellationToken);
        var publishedPosts = allPosts
            .Where(post => post.Status == PostStatus.Published)
            .OrderByDescending(post => post.PublishedAt)
            .ToList();
        return new BrowsePublishedPostsResult(publishedPosts);
    }
}
