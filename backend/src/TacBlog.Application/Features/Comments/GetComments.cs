using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Comments;

public sealed record GetCommentsResult(bool IsNotFound, int Count, IReadOnlyList<CommentDto> Comments)
{
    public static GetCommentsResult Success(IReadOnlyList<CommentDto> comments) =>
        new(false, comments.Count, comments);
    public static GetCommentsResult NotFound() =>
        new(true, 0, []);
}

public sealed class GetComments(
    IBlogPostRepository postRepository,
    ICommentRepository commentRepository)
{
    public async Task<GetCommentsResult> ExecuteAsync(
        string slugValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return GetCommentsResult.NotFound();

        var comments = await commentRepository.FindBySlugAsync(slug, cancellationToken);

        var dtos = comments
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new CommentDto(
                c.Id.Value,
                c.DisplayName,
                c.AvatarUrl,
                c.Provider.ToString(),
                c.Text.Value,
                c.CreatedAtUtc))
            .ToList();

        return GetCommentsResult.Success(dtos);
    }
}
