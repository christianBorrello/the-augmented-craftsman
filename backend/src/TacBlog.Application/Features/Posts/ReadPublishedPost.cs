using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record ReadPublishedPostResult(bool IsSuccess, BlogPost? Post)
{
    public static ReadPublishedPostResult Success(BlogPost post) => new(true, post);
    public static ReadPublishedPostResult NotFound() => new(false, null);
}

public sealed class ReadPublishedPost(IBlogPostRepository repository)
{
    public async Task<ReadPublishedPostResult> ExecuteAsync(string slug, CancellationToken cancellationToken = default)
    {
        Slug validatedSlug;
        try
        {
            validatedSlug = new Slug(slug);
        }
        catch (ArgumentException)
        {
            return ReadPublishedPostResult.NotFound();
        }

        var post = await repository.FindBySlugAsync(validatedSlug, cancellationToken);

        if (post is null || post.Status != PostStatus.Published)
            return ReadPublishedPostResult.NotFound();

        return ReadPublishedPostResult.Success(post);
    }
}
