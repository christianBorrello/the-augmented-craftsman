using System.Text.RegularExpressions;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Comments;

public sealed record PostCommentResult(bool IsSuccess, bool IsNotFound, bool IsUnauthorized, CommentDto? Comment, string? Error)
{
    public static PostCommentResult Success(CommentDto comment) => new(true, false, false, comment, null);
    public static PostCommentResult NotFound() => new(false, true, false, null, "Post not found");
    public static PostCommentResult Unauthorized() => new(false, false, true, null, "Authentication required");
    public static PostCommentResult ValidationError(string error) => new(false, false, false, null, error);
}

public sealed record CommentDto(
    Guid Id,
    string DisplayName,
    string? AvatarUrl,
    string Provider,
    string Text,
    DateTime CreatedAt);

public sealed class PostComment(
    IBlogPostRepository postRepository,
    ICommentRepository commentRepository,
    IReaderSessionRepository sessionRepository,
    IClock clock)
{
    public async Task<PostCommentResult> ExecuteAsync(
        string slugValue,
        string text,
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId is null)
            return PostCommentResult.Unauthorized();

        var session = await sessionRepository.FindByIdAsync(sessionId.Value, cancellationToken);
        if (session is null || session.IsExpired(clock.UtcNow))
            return PostCommentResult.Unauthorized();

        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return PostCommentResult.NotFound();

        var sanitizedText = SanitizeHtml(text);

        CommentText commentText;
        try
        {
            commentText = new CommentText(sanitizedText);
        }
        catch (ArgumentException ex)
        {
            return PostCommentResult.ValidationError(ex.Message);
        }

        var comment = Comment.Create(
            slug,
            session.DisplayName,
            session.AvatarUrl,
            session.Provider,
            commentText,
            clock.UtcNow);

        await commentRepository.SaveAsync(comment, cancellationToken);

        return PostCommentResult.Success(ToDto(comment));
    }

    private static string SanitizeHtml(string input) =>
        Regex.Replace(input, @"<[^>]*>", string.Empty);

    private static CommentDto ToDto(Comment comment) =>
        new(comment.Id.Value,
            comment.DisplayName,
            comment.AvatarUrl,
            comment.Provider.ToString(),
            comment.Text.Value,
            comment.CreatedAtUtc);
}
