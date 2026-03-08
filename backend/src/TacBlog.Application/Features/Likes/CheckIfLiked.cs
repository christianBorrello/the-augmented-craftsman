using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Likes;

public sealed record CheckIfLikedResult(bool IsNotFound, bool IsLiked, int Count)
{
    public static CheckIfLikedResult Found(bool isLiked, int count) => new(false, isLiked, count);
    public static CheckIfLikedResult NotFound() => new(true, false, 0);
}

public sealed class CheckIfLiked(IBlogPostRepository postRepository, ILikeRepository likeRepository)
{
    public async Task<CheckIfLikedResult> ExecuteAsync(
        string slugValue,
        string visitorIdValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return CheckIfLikedResult.NotFound();

        var visitorId = new VisitorId(Guid.Parse(visitorIdValue));

        var isLiked = await likeRepository.ExistsAsync(slug, visitorId, cancellationToken);
        var count = await likeRepository.CountBySlugAsync(slug, cancellationToken);

        return CheckIfLikedResult.Found(isLiked, count);
    }
}
