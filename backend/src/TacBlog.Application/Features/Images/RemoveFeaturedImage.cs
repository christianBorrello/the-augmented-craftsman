using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Images;

public sealed record RemoveFeaturedImageResult(bool IsSuccess, bool IsNotFound, BlogPost? Post)
{
    public static RemoveFeaturedImageResult Success(BlogPost post) => new(true, false, post);
    public static RemoveFeaturedImageResult NotFound() => new(false, true, null);
}

public sealed class RemoveFeaturedImage(IBlogPostRepository repository, IClock clock)
{
    public async Task<RemoveFeaturedImageResult> ExecuteAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var post = await repository.FindBySlugAsync(new Slug(slug), cancellationToken);

        if (post is null)
            return RemoveFeaturedImageResult.NotFound();

        post.RemoveFeaturedImage(clock.UtcNow);
        await repository.SaveAsync(post, cancellationToken);

        return RemoveFeaturedImageResult.Success(post);
    }
}
