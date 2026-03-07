using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;

namespace TacBlog.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        app.MapPost("/api/tags", CreateTagAsync).RequireAuthorization();
        app.MapGet("/api/tags", ListTagsAsync).AllowAnonymous();
        app.MapPut("/api/tags/{slug}", RenameTagAsync).RequireAuthorization();
        app.MapDelete("/api/tags/{slug}", DeleteTagAsync).RequireAuthorization();
        app.MapGet("/api/admin/tags", ListAllTagsAsync).RequireAuthorization();
    }

    private static async Task<IResult> CreateTagAsync(
        CreateTagRequest request,
        CreateTag createTag,
        CancellationToken cancellationToken)
    {
        var result = await createTag.ExecuteAsync(request.Name, cancellationToken);

        if (result.IsConflict)
            return Results.Conflict(new { error = result.ErrorMessage });

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = result.ErrorMessage });

        var response = ToResponse(result.Tag!);
        return Results.Created($"/api/tags/{response.Slug}", response);
    }

    private static async Task<IResult> ListTagsAsync(
        BrowsePublicTags browsePublicTags,
        CancellationToken cancellationToken)
    {
        var result = await browsePublicTags.ExecuteAsync(cancellationToken);
        return Results.Ok(result.Tags.Select(ToPublicTagResponse));
    }

    private static async Task<IResult> RenameTagAsync(
        string slug,
        RenameTagRequest request,
        RenameTag renameTag,
        CancellationToken cancellationToken)
    {
        var result = await renameTag.ExecuteAsync(slug, request.Name, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Tag not found" });

        if (result.IsConflict)
            return Results.Conflict(new { error = result.ErrorMessage });

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Ok(ToResponse(result.Tag!));
    }

    private static async Task<IResult> DeleteTagAsync(
        string slug,
        DeleteTag deleteTag,
        CancellationToken cancellationToken)
    {
        var result = await deleteTag.ExecuteAsync(slug, cancellationToken);

        if (result.IsNotFound)
            return Results.NotFound(new { error = "Tag not found" });

        return Results.NoContent();
    }

    private static async Task<IResult> ListAllTagsAsync(
        ListTags listTags,
        CancellationToken cancellationToken)
    {
        var result = await listTags.ExecuteAsync(cancellationToken);
        return Results.Ok(result.Tags.Select(t => ToResponse(t.Tag)));
    }

    private static TagResponse ToResponse(Domain.Tag tag) =>
        new(tag.Name.ToString(), tag.Slug.Value);

    private static TagWithPostCountResponse ToPublicTagResponse(PublicTagResult tag) =>
        new(tag.Name, tag.Slug, tag.PostCount);
}

public sealed record CreateTagRequest(string Name);

public sealed record RenameTagRequest(string Name);

public sealed record TagResponse(string Name, string Slug);

public sealed record TagWithPostCountResponse(string Name, string Slug, int PostCount);
