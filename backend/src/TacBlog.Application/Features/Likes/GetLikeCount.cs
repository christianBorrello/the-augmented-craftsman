using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Likes;

public sealed record GetLikeCountResult(bool IsSuccess, bool IsNotFound, int Count)
{
    public static GetLikeCountResult Success(int count) => new(true, false, count);
    public static GetLikeCountResult NotFound() => new(false, true, 0);
}

public sealed class GetLikeCount(IBlogPostRepository postRepository, ILikeRepository likeRepository)
{
    public async Task<GetLikeCountResult> ExecuteAsync(
        string slugValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return GetLikeCountResult.NotFound();

        var count = await likeRepository.CountBySlugAsync(slug, cancellationToken);
        return GetLikeCountResult.Success(count);
    }
}
