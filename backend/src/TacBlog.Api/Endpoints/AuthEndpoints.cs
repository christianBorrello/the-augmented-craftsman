using TacBlog.Application.Features.Auth;

namespace TacBlog.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", LoginAsync);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        LoginHandler loginHandler,
        CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.IsSuccess)
            return Results.Json(new { error = result.ErrorMessage }, statusCode: 401);

        return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }
}

public sealed record LoginRequest(string Email, string Password);
