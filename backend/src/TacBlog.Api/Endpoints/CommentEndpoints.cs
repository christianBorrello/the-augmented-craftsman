using TacBlog.Application.Features.Comments;

namespace TacBlog.Api.Endpoints;

public static class CommentEndpoints
{
    private const string SessionCookieName = "reader_session";

    public static void MapCommentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts/{slug}/comments", PostCommentAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}/comments", GetCommentsAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}/comments/count", GetCommentCountAsync).AllowAnonymous();
        app.MapDelete("/api/posts/{slug}/comments/{commentId:guid}", DeleteCommentAsync).RequireAuthorization();
        app.MapGet("/api/admin/comments", ListAdminCommentsAsync).RequireAuthorization();
    }

    private static async Task<IResult> PostCommentAsync(
        string slug,
        CommentRequest request,
        PostComment postComment,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Guid? sessionId = null;

        if (httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var cookieValue)
            && Guid.TryParse(cookieValue, out var parsed))
        {
            sessionId = parsed;
        }

        var result = await postComment.ExecuteAsync(slug, request.Text, sessionId, cancellationToken);

        if (result.IsUnauthorized)
            return Results.Json(new { error = result.Error }, statusCode: 401);

        if (result.IsNotFound)
            return Results.NotFound(new { error = result.Error });

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = result.Error });

        return Results.Created($"/api/posts/{slug}/comments/{result.Comment!.Id}", result.Comment);
    }

    private static async Task<IResult> GetCommentsAsync(
        string slug,
        GetComments getComments,
        CancellationToken cancellationToken)
    {
        var result = await getComments.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(new
        {
            count = result.Count,
            comments = result.Comments
        });
    }
    private static async Task<IResult> GetCommentCountAsync(
        string slug,
        GetCommentCount getCommentCount,
        CancellationToken cancellationToken)
    {
        var result = await getCommentCount.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(new { count = result.Count });
    }
    private static async Task<IResult> DeleteCommentAsync(
        string slug,
        Guid commentId,
        DeleteComment deleteComment,
        CancellationToken cancellationToken)
    {
        var result = await deleteComment.ExecuteAsync(commentId, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Comment not found" });

        return Results.NoContent();
    }

    private static async Task<IResult> ListAdminCommentsAsync(
        ListAdminComments listAdminComments,
        CancellationToken cancellationToken)
    {
        var comments = await listAdminComments.ExecuteAsync(cancellationToken);
        return Results.Ok(comments);
    }
}

public sealed record CommentRequest(string Text);
