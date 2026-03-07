using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts", CreatePostAsync).RequireAuthorization();
        app.MapGet("/api/posts", ListPublishedPostsAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}", GetPostBySlugAsync).AllowAnonymous();
        app.MapPut("/api/posts/{id:guid}", EditPostAsync).RequireAuthorization();
        app.MapDelete("/api/posts/{id:guid}", DeletePostAsync).RequireAuthorization();
        app.MapPost("/api/posts/{id:guid}/publish", PublishPostAsync).RequireAuthorization();
        app.MapGet("/api/posts/{id:guid}/preview", PreviewPostAsync).RequireAuthorization();
        app.MapGet("/api/admin/posts", ListAllPostsAsync).RequireAuthorization();
    }

    private static async Task<IResult> ListPublishedPostsAsync(
        IBlogPostRepository repository,
        CancellationToken cancellationToken)
    {
        var posts = await repository.FindAllAsync(cancellationToken);
        return Results.Ok(posts.Select(ToResponse));
    }

    private static async Task<IResult> CreatePostAsync(
        CreatePostRequest request,
        CreatePost createPost,
        CancellationToken cancellationToken)
    {
        var result = await createPost.ExecuteAsync(
            request.Title,
            request.Content,
            request.Tags?.ToList(),
            cancellationToken);

        if (result.IsConflict)
            return Results.Conflict(new { error = result.ErrorMessage });

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = ToUserFacingMessage(result.ErrorMessage!) });

        var response = ToResponse(result.Post!);
        return Results.Created($"/api/posts/{response.Slug}", response);
    }

    private static async Task<IResult> GetPostBySlugAsync(
        string slug,
        GetPostBySlug getPostBySlug,
        CancellationToken cancellationToken)
    {
        var result = await getPostBySlug.ExecuteAsync(slug, cancellationToken);

        if (!result.IsSuccess)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(ToResponse(result.Post!));
    }

    private static async Task<IResult> EditPostAsync(
        Guid id,
        EditPostRequest request,
        EditPost editPost,
        CancellationToken cancellationToken)
    {
        var result = await editPost.ExecuteAsync(
            id,
            request.Title,
            request.Content,
            request.Tags?.ToList(),
            cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = ToUserFacingMessage(result.ErrorMessage!) });

        return Results.Ok(ToResponse(result.Post!));
    }

    private static async Task<IResult> DeletePostAsync(
        Guid id,
        DeletePost deletePost,
        CancellationToken cancellationToken)
    {
        var result = await deletePost.ExecuteAsync(id, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.NoContent();
    }

    private static async Task<IResult> PublishPostAsync(
        Guid id,
        PublishPost publishPost,
        CancellationToken cancellationToken)
    {
        var result = await publishPost.ExecuteAsync(id, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        if (result.IsConflict)
            return Results.Conflict(new { error = result.ErrorMessage });

        return Results.Ok(ToResponse(result.Post!));
    }

    private static async Task<IResult> PreviewPostAsync(
        Guid id,
        PreviewPost previewPost,
        CancellationToken cancellationToken)
    {
        var result = await previewPost.ExecuteAsync(id, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(ToResponse(result.Post!));
    }

    private static async Task<IResult> ListAllPostsAsync(
        ListPosts listPosts,
        CancellationToken cancellationToken)
    {
        var result = await listPosts.ExecuteAsync(cancellationToken);
        return Results.Ok(result.Posts.Select(ToResponse));
    }

    private static string ToUserFacingMessage(string errorMessage) =>
        errorMessage switch
        {
            _ when errorMessage.Contains("Title cannot be empty") => "Title is required",
            _ when errorMessage.Contains("Content cannot be empty") => "Content is required",
            _ => errorMessage
        };

    private static PostResponse ToResponse(BlogPost post) =>
        new(
            Id: post.Id.Value,
            Title: post.Title.Value,
            Slug: post.Slug.Value,
            Content: post.Content.Value,
            Status: post.Status.ToString(),
            CreatedAt: post.CreatedAt,
            UpdatedAt: post.UpdatedAt,
            PublishedAt: post.PublishedAt?.ToString("o"),
            Tags: post.Tags.Select(t => t.Name.ToString()).ToArray());
}

public sealed record CreatePostRequest(string Title, string Content, string[]? Tags = null);

public sealed record EditPostRequest(string Title, string Content, string[]? Tags = null);

public sealed record PostResponse(
    Guid Id,
    string Title,
    string Slug,
    string Content,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? PublishedAt = null,
    string? FeaturedImageUrl = null,
    string[]? Tags = null);
