using TacBlog.Application.Features.OAuth;

namespace TacBlog.Api.Endpoints;

public static class OAuthEndpoints
{
    private const string SessionCookieName = "reader_session";

    public static void MapOAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/oauth/{provider}/callback", HandleCallbackAsync).AllowAnonymous();
        app.MapGet("/api/auth/session", CheckSessionAsync).AllowAnonymous();
    }

    private static async Task<IResult> HandleCallbackAsync(
        string provider,
        string code,
        string? state,
        HandleOAuthCallback handleOAuthCallback,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var redirectUri = BuildRedirectUri(httpContext, provider);
        var result = await handleOAuthCallback.ExecuteAsync(provider, code, redirectUri, cancellationToken);

        var returnUrl = state ?? "/";

        if (!result.IsSuccess)
            return Results.Redirect($"{returnUrl}?error={result.Error}");

        httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId!.Value.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        });

        return Results.Redirect(returnUrl);
    }

    private static async Task<IResult> CheckSessionAsync(
        CheckSession checkSession,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Guid? sessionId = null;

        if (httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var cookieValue)
            && Guid.TryParse(cookieValue, out var parsed))
        {
            sessionId = parsed;
        }

        var result = await checkSession.ExecuteAsync(sessionId, cancellationToken);

        return Results.Ok(new
        {
            authenticated = result.IsAuthenticated,
            displayName = result.DisplayName,
            avatarUrl = result.AvatarUrl,
            provider = result.Provider
        });
    }

    private static string BuildRedirectUri(HttpContext httpContext, string provider)
    {
        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host}/api/auth/oauth/{provider}/callback";
    }
}
