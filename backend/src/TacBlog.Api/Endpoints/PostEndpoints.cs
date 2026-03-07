using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts", CreatePostAsync).RequireAuthorization();
        app.MapGet("/api/posts", ListPostsAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}", GetPostBySlugAsync).AllowAnonymous();
    }

    private static async Task<IResult> ListPostsAsync(
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
        var result = await createPost.ExecuteAsync(request.Title, request.Content, cancellationToken: cancellationToken);

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
            return Results.NotFound();

        return Results.Ok(ToResponse(result.Post!));
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

public sealed record CreatePostRequest(string Title, string Content);

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
