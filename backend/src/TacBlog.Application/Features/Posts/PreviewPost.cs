using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record PreviewPostResult(bool IsSuccess, bool IsNotFound, BlogPost? Post)
{
    public static PreviewPostResult Success(BlogPost post) => new(true, false, post);
    public static PreviewPostResult NotFound() => new(false, true, null);
}

public sealed class PreviewPost(IBlogPostRepository repository)
{
    public async Task<PreviewPostResult> ExecuteAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var post = await repository.FindByIdAsync(new PostId(postId), cancellationToken);

        if (post is null)
            return PreviewPostResult.NotFound();

        return PreviewPostResult.Success(post);
    }
}
