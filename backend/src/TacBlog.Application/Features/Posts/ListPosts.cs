using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record ListPostsResult(IReadOnlyList<BlogPost> Posts);

public sealed class ListPosts(IBlogPostRepository repository)
{
    public async Task<ListPostsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var allPosts = await repository.FindAllAsync(cancellationToken);
        var sorted = allPosts.OrderByDescending(post => post.CreatedAt).ToList();
        return new ListPostsResult(sorted);
    }
}
