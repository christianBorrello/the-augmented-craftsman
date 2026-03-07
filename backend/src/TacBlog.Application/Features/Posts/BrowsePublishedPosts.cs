using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record BrowsePublishedPostsResult(IReadOnlyList<BlogPost> Posts);

public sealed class BrowsePublishedPosts(IBlogPostRepository repository)
{
    public async Task<BrowsePublishedPostsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var publishedPosts = await repository.FindPublishedAsync(cancellationToken);
        return new BrowsePublishedPostsResult(publishedPosts);
    }
}
