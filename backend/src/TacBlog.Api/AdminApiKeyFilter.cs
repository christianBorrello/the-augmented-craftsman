using Microsoft.Extensions.Configuration;

namespace TacBlog.Api;

public sealed class AdminApiKeyFilter(IConfiguration configuration) : IEndpointFilter
{
    private const string HeaderName = "X-Admin-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configured = configuration["Admin:ApiKey"];
        if (string.IsNullOrEmpty(configured))
            return Results.StatusCode(503);

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided)
            || provided != configured)
            return Results.Unauthorized();

        return await next(context);
    }
}
