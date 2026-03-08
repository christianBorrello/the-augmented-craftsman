using TacBlog.Application.Features.Likes;

namespace TacBlog.Api.Endpoints;

public static class LikeEndpoints
{
    public static void MapLikeEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts/{slug}/likes", LikePostAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}/likes/count", GetLikeCountAsync).AllowAnonymous();
    }

    private static async Task<IResult> LikePostAsync(
        string slug,
        LikeRequest request,
        LikePost likePost,
        CancellationToken cancellationToken)
    {
        var result = await likePost.ExecuteAsync(slug, request.VisitorId, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(new { liked = true, count = result.Count });
    }

    private static async Task<IResult> GetLikeCountAsync(
        string slug,
        GetLikeCount getLikeCount,
        CancellationToken cancellationToken)
    {
        var result = await getLikeCount.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(new { count = result.Count });
    }
}

public sealed record LikeRequest(string VisitorId);
