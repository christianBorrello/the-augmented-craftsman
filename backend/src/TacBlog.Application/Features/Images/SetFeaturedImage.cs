using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Images;

public sealed record SetFeaturedImageResult(
    bool IsSuccess,
    bool IsNotFound,
    bool IsValidationError,
    BlogPost? Post,
    string? ErrorMessage)
{
    public static SetFeaturedImageResult Success(BlogPost post) => new(true, false, false, post, null);
    public static SetFeaturedImageResult NotFound() => new(false, true, false, null, null);
    public static SetFeaturedImageResult ValidationError(string message) => new(false, false, true, null, message);
}

public sealed class SetFeaturedImage(IBlogPostRepository repository, IClock clock)
{
    public async Task<SetFeaturedImageResult> ExecuteAsync(
        string slug,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        FeaturedImageUrl featuredImageUrl;
        try
        {
            featuredImageUrl = new FeaturedImageUrl(imageUrl);
        }
        catch (ArgumentException exception)
        {
            return SetFeaturedImageResult.ValidationError(exception.Message);
        }

        var post = await repository.FindBySlugAsync(new Slug(slug), cancellationToken);

        if (post is null)
            return SetFeaturedImageResult.NotFound();

        post.SetFeaturedImage(featuredImageUrl, clock.UtcNow);
        await repository.SaveAsync(post, cancellationToken);

        return SetFeaturedImageResult.Success(post);
    }
}
