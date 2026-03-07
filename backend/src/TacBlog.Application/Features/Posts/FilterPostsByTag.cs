using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record FilterPostsByTagResult(bool IsSuccess, bool IsNotFound, IReadOnlyList<BlogPost>? Posts)
{
    public static FilterPostsByTagResult Success(IReadOnlyList<BlogPost> posts) => new(true, false, posts);
    public static FilterPostsByTagResult NotFound() => new(false, true, null);
}

public sealed class FilterPostsByTag(IBlogPostRepository repository)
{
    public async Task<FilterPostsByTagResult> ExecuteAsync(string tagSlug, CancellationToken cancellationToken = default)
    {
        Slug validatedSlug;
        try
        {
            validatedSlug = new Slug(tagSlug);
        }
        catch (ArgumentException)
        {
            return FilterPostsByTagResult.NotFound();
        }

        var tagExists = await repository.FindTagBySlugAsync(validatedSlug, cancellationToken);

        if (tagExists is null)
            return FilterPostsByTagResult.NotFound();

        var posts = await repository.FindPublishedByTagSlugAsync(validatedSlug, cancellationToken);
        return FilterPostsByTagResult.Success(posts);
    }
}
