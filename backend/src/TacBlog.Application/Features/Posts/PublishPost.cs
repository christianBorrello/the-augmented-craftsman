using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record PublishPostResult(bool IsSuccess, bool IsNotFound, bool IsConflict, BlogPost? Post, string? ErrorMessage)
{
    public static PublishPostResult Success(BlogPost post) => new(true, false, false, post, null);
    public static PublishPostResult NotFound() => new(false, true, false, null, null);
    public static PublishPostResult Conflict(string message) => new(false, false, true, null, message);
}

public sealed class PublishPost(IBlogPostRepository repository, IClock clock)
{
    public async Task<PublishPostResult> ExecuteAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var post = await repository.FindByIdAsync(new PostId(postId), cancellationToken);

        if (post is null)
            return PublishPostResult.NotFound();

        try
        {
            post.Publish(clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return PublishPostResult.Conflict("Post is already published");
        }

        await repository.SaveAsync(post, cancellationToken);

        return PublishPostResult.Success(post);
    }
}
