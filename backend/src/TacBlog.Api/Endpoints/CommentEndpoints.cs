using TacBlog.Application.Features.Comments;

namespace TacBlog.Api.Endpoints;

public static class CommentEndpoints
{
    private const string SessionCookieName = "reader_session";

    public static void MapCommentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts/{slug}/comments", PostCommentAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}/comments", GetCommentsAsync).AllowAnonymous();
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
}

public sealed record CommentRequest(string Text);
