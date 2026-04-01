using Microsoft.AspNetCore.Mvc;
using TacBlog.Api;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        app.MapPost("/api/posts", CreatePostAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapGet("/api/posts", BrowsePostsAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}", ReadPostBySlugAsync).AllowAnonymous();
        app.MapGet("/api/posts/{slug}/related", GetRelatedPostsAsync).AllowAnonymous();
        app.MapPut("/api/posts/{id:guid}", EditPostAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapDelete("/api/posts/{id:guid}", DeletePostAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapPost("/api/posts/{id:guid}/publish", PublishPostAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapPost("/api/posts/{id:guid}/schedule", SchedulePostAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapPost("/api/posts/publish-scheduled", PublishScheduledPostsAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapGet("/api/posts/{id:guid}/preview", PreviewPostAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapGet("/api/admin/posts", ListAllPostsAsync).AddEndpointFilter<AdminApiKeyFilter>();
        app.MapGet("/api/admin/posts/{slug}", GetAdminPostBySlugAsync).AddEndpointFilter<AdminApiKeyFilter>();
    }

    private static async Task<IResult> BrowsePostsAsync(
        string? tag,
        BrowsePublishedPosts browsePublishedPosts,
        FilterPostsByTag filterPostsByTag,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            var filterResult = await filterPostsByTag.ExecuteAsync(tag, cancellationToken);

            if (filterResult.IsNotFound)
                return Results.NotFound(new { error = "Tag not found" });

            return Results.Ok(filterResult.Posts!.Select(ToSummaryResponse));
        }

        var result = await browsePublishedPosts.ExecuteAsync(cancellationToken);
        return Results.Ok(result.Posts.Select(ToSummaryResponse));
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

    private static async Task<IResult> ReadPostBySlugAsync(
        string slug,
        ReadPublishedPost readPublishedPost,
        CancellationToken cancellationToken)
    {
        var result = await readPublishedPost.ExecuteAsync(slug, cancellationToken);

        if (!result.IsSuccess)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(ToResponse(result.Post!));
    }

    private static async Task<IResult> GetRelatedPostsAsync(
        string slug,
        GetRelatedPosts getRelatedPosts,
        CancellationToken cancellationToken)
    {
        var result = await getRelatedPosts.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        return Results.Ok(result.Posts!.Select(ToSummaryResponse));
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
        DateTime? publishAt,
        PublishPost publishPost,
        CancellationToken cancellationToken)
    {
        var result = await publishPost.ExecuteAsync(id, publishAt, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Post not found" });

        if (result.IsConflict)
            return Results.Conflict(new { error = result.ErrorMessage });

        return Results.Ok(ToResponse(result.Post!));
    }

    private static async Task<IResult> SchedulePostAsync(
        Guid id,
        DateTime at,
        IBlogPostRepository repository,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var post = await repository.FindByIdAsync(new PostId(id), cancellationToken);

        if (post is null)
            return Results.NotFound(new { error = "Post not found" });

        try
        {
            post.Schedule(at, clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        await repository.SaveAsync(post, cancellationToken);
        return Results.Ok(ToResponse(post));
    }

    private static async Task<IResult> PublishScheduledPostsAsync(
        [FromServices] PublishScheduledPosts publishScheduledPosts,
        CancellationToken cancellationToken)
    {
        var result = await publishScheduledPosts.ExecuteAsync(cancellationToken);
        return Results.Ok(new { published = result.PublishedSlugs });
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

    private static async Task<IResult> GetAdminPostBySlugAsync(
        string slug,
        GetPostBySlug getPostBySlug,
        CancellationToken cancellationToken)
    {
        var result = await getPostBySlug.ExecuteAsync(slug, cancellationToken);

        if (!result.IsSuccess)
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

    private static string ToUserFacingMessage(string errorMessage)
    {
        if (errorMessage.Contains("Title cannot be empty"))
            return "Title is required";

        if (errorMessage.Contains("Content cannot be empty"))
            return "Content is required";

        return errorMessage;
    }

    private static PostTagResponse ToPostTagResponse(Tag tag) =>
        new(tag.Name.ToString(), tag.Slug.Value, tag.Color.Light, tag.Color.Dark);

    internal static PostResponse ToResponse(BlogPost post) =>
        new(
            Id: post.Id.Value,
            Title: post.Title.Value,
            Slug: post.Slug.Value,
            Content: post.Content.Value,
            Status: post.Status.ToString(),
            CreatedAt: post.CreatedAt,
            UpdatedAt: post.UpdatedAt,
            PublishedAt: post.PublishedAt?.ToString("o"),
            FeaturedImageUrl: post.FeaturedImageUrl?.Value,
            Tags: post.Tags.Select(ToPostTagResponse).ToArray());

    private static PostSummaryResponse ToSummaryResponse(BlogPost post) =>
        new(
            Title: post.Title.Value,
            Slug: post.Slug.Value,
            PublishedAt: post.PublishedAt?.ToString("o"),
            FeaturedImageUrl: post.FeaturedImageUrl?.Value,
            Tags: post.Tags.Select(ToPostTagResponse).ToArray());
}

public sealed record CreatePostRequest(string Title, string Content, string[]? Tags = null);

public sealed record EditPostRequest(string Title, string Content, string[]? Tags = null);

public sealed record PostTagResponse(string Name, string Slug, string Color, string ColorDark);

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
    PostTagResponse[]? Tags = null);

public sealed record PostSummaryResponse(
    string Title,
    string Slug,
    string? PublishedAt,
    string? FeaturedImageUrl,
    PostTagResponse[]? Tags);
