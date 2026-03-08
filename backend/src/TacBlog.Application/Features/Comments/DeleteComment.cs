using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Comments;

public sealed record DeleteCommentResult(bool IsSuccess, bool IsNotFound)
{
    public static DeleteCommentResult Success() => new(true, false);
    public static DeleteCommentResult NotFound() => new(false, true);
}

public sealed class DeleteComment(ICommentRepository commentRepository)
{
    public async Task<DeleteCommentResult> ExecuteAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var id = new CommentId(commentId);
        var comment = await commentRepository.FindByIdAsync(id, cancellationToken);

        if (comment is null)
            return DeleteCommentResult.NotFound();

        await commentRepository.DeleteAsync(id, cancellationToken);
        return DeleteCommentResult.Success();
    }
}
