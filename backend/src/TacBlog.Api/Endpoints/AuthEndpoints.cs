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

        if (result.IsLockedOut)
            return Results.Json(new ErrorResponse(result.ErrorMessage!), statusCode: 429);

        if (!result.IsSuccess)
            return Results.Json(new ErrorResponse(result.ErrorMessage!), statusCode: 401);

        return Results.Ok(new LoginResponse(result.Token!, result.ExpiresAt!.Value));
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, DateTime ExpiresAt);
public sealed record ErrorResponse(string Error);
