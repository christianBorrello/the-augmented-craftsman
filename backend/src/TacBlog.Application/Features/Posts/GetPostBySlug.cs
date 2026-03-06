using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record GetPostBySlugResult(bool IsSuccess, BlogPost? Post)
{
    public static GetPostBySlugResult Success(BlogPost post) => new(true, post);
    public static GetPostBySlugResult NotFound() => new(false, null);
}

public sealed class GetPostBySlug(IBlogPostRepository repository)
{
    public async Task<GetPostBySlugResult> ExecuteAsync(string slug, CancellationToken cancellationToken = default)
    {
        Slug validatedSlug;
        try
        {
            validatedSlug = new Slug(slug);
        }
        catch (ArgumentException)
        {
            return GetPostBySlugResult.NotFound();
        }

        var post = await repository.FindBySlugAsync(validatedSlug, cancellationToken);

        return post is not null
            ? GetPostBySlugResult.Success(post)
            : GetPostBySlugResult.NotFound();
    }
}
