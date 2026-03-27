namespace TacBlog.Api.Endpoints;

public static class LocalEndpoints
{
    public static IEndpointRouteBuilder MapLocalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/local/file", async (SaveFileRequest req) =>
        {
            if (!req.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Only .md files are allowed.");

            if (req.FilePath.Contains(".."))
                return Results.BadRequest("Path traversal is not allowed.");

            if (!File.Exists(req.FilePath))
                return Results.NotFound("File not found. Will not create new files via this endpoint.");

            await File.WriteAllTextAsync(req.FilePath, req.Content);
            return Results.Ok();
        })
        .AddEndpointFilter<AdminApiKeyFilter>();

        return app;
    }
}

record SaveFileRequest(string FilePath, string Content);
