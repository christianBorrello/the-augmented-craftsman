using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record GetRelatedPostsResult(bool IsSuccess, bool IsNotFound, IReadOnlyList<BlogPost>? Posts)
{
    public static GetRelatedPostsResult Success(IReadOnlyList<BlogPost> posts) => new(true, false, posts);
    public static GetRelatedPostsResult NotFound() => new(false, true, null);
}

public sealed class GetRelatedPosts(IBlogPostRepository repository)
{
    private const int MaxRelatedPosts = 3;

    public async Task<GetRelatedPostsResult> ExecuteAsync(string slug, CancellationToken cancellationToken = default)
    {
        Slug validatedSlug;
        try
        {
            validatedSlug = new Slug(slug);
        }
        catch (ArgumentException)
        {
            return GetRelatedPostsResult.NotFound();
        }

        var sourcePost = await repository.FindBySlugAsync(validatedSlug, cancellationToken);

        if (sourcePost is null)
            return GetRelatedPostsResult.NotFound();

        var sourceTagIds = sourcePost.Tags.Select(tag => tag.Id).ToHashSet();

        var allPosts = await repository.FindAllAsync(cancellationToken);

        var relatedPosts = allPosts
            .Where(post => post.Id != sourcePost.Id)
            .Where(post => post.Status == PostStatus.Published)
            .Select(post => new
            {
                Post = post,
                SharedTagCount = post.Tags.Count(tag => sourceTagIds.Contains(tag.Id))
            })
            .Where(entry => entry.SharedTagCount > 0)
            .OrderByDescending(entry => entry.SharedTagCount)
            .ThenByDescending(entry => entry.Post.PublishedAt)
            .Take(MaxRelatedPosts)
            .Select(entry => entry.Post)
            .ToList();

        return GetRelatedPostsResult.Success(relatedPosts);
    }
}
