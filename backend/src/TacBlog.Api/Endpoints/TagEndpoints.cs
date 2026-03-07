using Microsoft.EntityFrameworkCore;
using TacBlog.Infrastructure;

namespace TacBlog.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tags", ListTagsAsync).AllowAnonymous();
    }

    private static async Task<IResult> ListTagsAsync(
        TacBlogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tags = await dbContext.Tags
            .OrderBy(t => t.Name)
            .Select(t => new { name = t.Name.Value, slug = t.Slug.Value })
            .ToListAsync(cancellationToken);

        return Results.Ok(tags);
    }
}
