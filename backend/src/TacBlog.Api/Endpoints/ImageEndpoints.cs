using TacBlog.Application.Features.Images;

namespace TacBlog.Api.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapPost("/api/images", UploadImageAsync).RequireAuthorization().DisableAntiforgery();
        app.MapPut("/api/posts/{slug}/featured-image", SetFeaturedImageAsync).RequireAuthorization();
        app.MapDelete("/api/posts/{slug}/featured-image", RemoveFeaturedImageAsync).RequireAuthorization();
    }

    private static async Task<IResult> UploadImageAsync(
        IFormFile file,
        UploadImage uploadImage,
        CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        var result = await uploadImage.ExecuteAsync(
            stream, file.FileName, file.ContentType, cancellationToken);

        if (result.IsServiceUnavailable)
            return Results.StatusCode(503);

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Created(result.Url, new { url = result.Url });
    }

    private static async Task<IResult> SetFeaturedImageAsync(
        string slug,
        SetFeaturedImageRequest request,
        SetFeaturedImage setFeaturedImage,
        CancellationToken cancellationToken)
    {
        var result = await setFeaturedImage.ExecuteAsync(
            slug, request.ImageUrl, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        if (result.IsValidationError)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Ok(PostEndpoints.ToResponse(result.Post!));
    }

    private static async Task<IResult> RemoveFeaturedImageAsync(
        string slug,
        RemoveFeaturedImage removeFeaturedImage,
        CancellationToken cancellationToken)
    {
        var result = await removeFeaturedImage.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(PostEndpoints.ToResponse(result.Post!));
    }
}

public sealed record SetFeaturedImageRequest(string ImageUrl);
