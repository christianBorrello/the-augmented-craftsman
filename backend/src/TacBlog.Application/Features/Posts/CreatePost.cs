using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record CreatePostResult(bool IsSuccess, BlogPost? Post, string? ErrorMessage)
{
    public static CreatePostResult Success(BlogPost post) => new(true, post, null);
    public static CreatePostResult ValidationError(string message) => new(false, null, message);
}

public sealed class CreatePost(IBlogPostRepository repository, IClock clock)
{
    public async Task<CreatePostResult> ExecuteAsync(string title, string content, CancellationToken cancellationToken = default)
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

        await repository.SaveAsync(post, cancellationToken);

        return CreatePostResult.Success(post);
    }
}
