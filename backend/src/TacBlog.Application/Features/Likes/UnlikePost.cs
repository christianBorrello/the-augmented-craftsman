using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Likes;

public sealed record UnlikePostResult(bool IsNotFound, int Count)
{
    public static UnlikePostResult Success(int count) => new(false, count);
    public static UnlikePostResult NotFound() => new(true, 0);
}

public sealed class UnlikePost(IBlogPostRepository postRepository, ILikeRepository likeRepository)
{
    public async Task<UnlikePostResult> ExecuteAsync(
        string slugValue,
        string visitorIdValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return UnlikePostResult.NotFound();

        var visitorId = new VisitorId(Guid.Parse(visitorIdValue));

        await likeRepository.DeleteAsync(slug, visitorId, cancellationToken);

        var count = await likeRepository.CountBySlugAsync(slug, cancellationToken);
        return UnlikePostResult.Success(count);
    }
}
