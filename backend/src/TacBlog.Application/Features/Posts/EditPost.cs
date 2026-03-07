using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

public sealed record EditPostResult(bool IsSuccess, bool IsNotFound, BlogPost? Post, string? ErrorMessage)
{
    public static EditPostResult Success(BlogPost post) => new(true, false, post, null);
    public static EditPostResult NotFound() => new(false, true, null, null);
    public static EditPostResult ValidationError(string message) => new(false, false, null, message);
}

public sealed class EditPost(IBlogPostRepository repository, IClock clock)
{
    public async Task<EditPostResult> ExecuteAsync(
        Guid postId,
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
            return EditPostResult.ValidationError(exception.Message);
        }

        var post = await repository.FindByIdAsync(new PostId(postId), cancellationToken);

        if (post is null)
            return EditPostResult.NotFound();

        var now = clock.UtcNow;
        post.UpdateTitle(validatedTitle, now);
        post.UpdateContent(new PostContent(content), now);

        post.ClearTags();
        if (tagNames is not null)
        {
            foreach (var name in tagNames)
                post.AddTag(Tag.Create(new TagName(name)));
        }

        await repository.SaveAsync(post, cancellationToken);

        return EditPostResult.Success(post);
    }
}
