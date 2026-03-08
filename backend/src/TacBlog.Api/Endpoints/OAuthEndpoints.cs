using TacBlog.Application.Features.OAuth;

namespace TacBlog.Api.Endpoints;

public static class OAuthEndpoints
{
    private const string SessionCookieName = "reader_session";

    public static void MapOAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/oauth/{provider}", InitiateOAuthAsync).AllowAnonymous();
        app.MapGet("/api/auth/oauth/{provider}/callback", HandleCallbackAsync).AllowAnonymous();
        app.MapGet("/api/auth/session", CheckSessionAsync).AllowAnonymous();
        app.MapPost("/api/auth/signout", SignOutAsync).AllowAnonymous();
    }

    private static async Task<IResult> InitiateOAuthAsync(
        string provider,
        string? returnUrl,
        InitiateOAuth initiateOAuth,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var state = returnUrl ?? "/";
        var redirectUri = BuildRedirectUri(httpContext, provider);
        var result = await initiateOAuth.ExecuteAsync(provider, state, redirectUri, cancellationToken);

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = result.Error });

        return Results.Redirect(result.AuthorizationUrl!);
    }

    private static async Task<IResult> HandleCallbackAsync(
        string provider,
        string code,
        string? state,
        HandleOAuthCallback handleOAuthCallback,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Results.Redirect("/?error=invalid_state");

        var redirectUri = BuildRedirectUri(httpContext, provider);
        var result = await handleOAuthCallback.ExecuteAsync(provider, code, redirectUri, cancellationToken);

        var returnUrl = state;

        if (!result.IsSuccess)
        {
            if (result.Error == "access_denied")
                return Results.Redirect(returnUrl);

            return Results.Redirect($"{returnUrl}?error={result.Error}");
        }

        httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId!.Value.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
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

    private static async Task<IResult> SignOutAsync(
        SignOut signOut,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Guid? sessionId = null;

        if (httpContext.Request.Cookies.TryGetValue(SessionCookieName, out var cookieValue)
            && Guid.TryParse(cookieValue, out var parsed))
        {
            sessionId = parsed;
        }

        await signOut.ExecuteAsync(sessionId, cancellationToken);

        httpContext.Response.Cookies.Delete(SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        return Results.NoContent();
    }

    private static string BuildRedirectUri(HttpContext httpContext, string provider)
    {
        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host}/api/auth/oauth/{provider}/callback";
    }
}
