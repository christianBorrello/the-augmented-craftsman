using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Comments;

public sealed record GetCommentCountResult(bool IsNotFound, int Count)
{
    public static GetCommentCountResult Success(int count) => new(false, count);
    public static GetCommentCountResult NotFound() => new(true, 0);
}

public sealed class GetCommentCount(
    IBlogPostRepository postRepository,
    ICommentRepository commentRepository)
{
    public async Task<GetCommentCountResult> ExecuteAsync(
        string slugValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return GetCommentCountResult.NotFound();

        var count = await commentRepository.CountBySlugAsync(slug, cancellationToken);
        return GetCommentCountResult.Success(count);
    }
}
