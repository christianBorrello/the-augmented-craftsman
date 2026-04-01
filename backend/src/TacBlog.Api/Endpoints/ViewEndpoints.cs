using TacBlog.Application.Features.Views;

namespace TacBlog.Api.Endpoints;

public static class ViewEndpoints
{
    public static void MapViewEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts/{slug}/views", RecordViewAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}/views/count", GetViewCountAsync).AllowAnonymous();
    }

    private static async Task<IResult> RecordViewAsync(
        string slug,
        ViewRequest request,
        RecordPageView recordPageView,
        CancellationToken cancellationToken)
    {
        if (!IsValidVisitorId(request.VisitorId))
            return Results.BadRequest(new { error = "Invalid visitor identifier" });

        var result = await recordPageView.ExecuteAsync(slug, request.VisitorId, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(new { count = result.Count });
    }

    private static async Task<IResult> GetViewCountAsync(
        string slug,
        GetPageViewCount getPageViewCount,
        CancellationToken cancellationToken)
    {
        var result = await getPageViewCount.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(new { count = result.Count });
    }

    private static bool IsValidVisitorId(string? visitorId) =>
        !string.IsNullOrWhiteSpace(visitorId)
        && Guid.TryParse(visitorId, out var parsed)
        && parsed != Guid.Empty;
}

public sealed record ViewRequest(string VisitorId);
