using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record CreatePostResult(bool IsSuccess, bool IsConflict, BlogPost? Post, string? ErrorMessage)
{
    public static CreatePostResult Success(BlogPost post) => new(true, false, post, null);
    public static CreatePostResult ValidationError(string message) => new(false, false, null, message);
    public static CreatePostResult Conflict(string message) => new(false, true, null, message);
}

public sealed class CreatePost(IBlogPostRepository repository, IClock clock)
{
    public async Task<CreatePostResult> ExecuteAsync(
        string title,
        string content,
        IReadOnlyList<string>? tagNames = null,
        CancellationToken cancellationToken = default)
    {
        Title validatedTitle;
        try
        {
            validatedTitle = new Title(title);
        }
        catch (ArgumentException exception)
        {
            return CreatePostResult.ValidationError(exception.Message);
        }

        PostContent validatedContent;
        try
        {
            validatedContent = new PostContent(content);
        }
        catch (ArgumentException exception)
        {
            return CreatePostResult.ValidationError(exception.Message);
        }

        var post = BlogPost.Create(validatedTitle, validatedContent, clock.UtcNow);

        if (await repository.ExistsBySlugAsync(post.Slug, cancellationToken))
            return CreatePostResult.Conflict("A post with this URL already exists");

        if (tagNames is not null)
        {
            foreach (var name in tagNames)
            {
                var tagName = new TagName(name);
                var slug = Slug.FromTagName(tagName);
                var existingTag = await repository.FindTagBySlugAsync(slug, cancellationToken);
                post.AddTag(existingTag ?? Tag.Create(tagName));
            }
        }

        await repository.SaveAsync(post, cancellationToken);

        return CreatePostResult.Success(post);
    }
}
