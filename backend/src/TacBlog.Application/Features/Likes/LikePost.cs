using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Likes;

public sealed record LikePostResult(bool IsSuccess, bool IsNotFound, int Count, string? ErrorMessage)
{
    public static LikePostResult Success(int count) => new(true, false, count, null);
    public static LikePostResult NotFound() => new(false, true, 0, "Post not found");
}

public sealed class LikePost(IBlogPostRepository postRepository, ILikeRepository likeRepository, IClock clock)
{
    public async Task<LikePostResult> ExecuteAsync(
        string slugValue,
        string visitorIdValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return LikePostResult.NotFound();

        var visitorId = new VisitorId(Guid.Parse(visitorIdValue));

        var alreadyLiked = await likeRepository.ExistsAsync(slug, visitorId, cancellationToken);
        if (!alreadyLiked)
        {
            var like = Like.Create(slug, visitorId, clock.UtcNow);
            await likeRepository.SaveAsync(like, cancellationToken);
        }

        var count = await likeRepository.CountBySlugAsync(slug, cancellationToken);
        return LikePostResult.Success(count);
    }
}
