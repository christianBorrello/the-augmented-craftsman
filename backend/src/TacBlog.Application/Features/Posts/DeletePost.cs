using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record DeletePostResult(bool IsSuccess, bool IsNotFound)
{
    public static DeletePostResult Success() => new(true, false);
    public static DeletePostResult NotFound() => new(false, true);
}

public sealed class DeletePost(IBlogPostRepository repository)
{
    public async Task<DeletePostResult> ExecuteAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var post = await repository.FindByIdAsync(new PostId(postId), cancellationToken);

        if (post is null)
            return DeletePostResult.NotFound();

        await repository.DeleteAsync(post.Id, cancellationToken);

        return DeletePostResult.Success();
    }
}
