using Microsoft.Extensions.Configuration;
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
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var state = returnUrl ?? "/";
        var redirectUri = BuildRedirectUri(httpContext, configuration, provider);
        var result = await initiateOAuth.ExecuteAsync(provider, state, redirectUri, cancellationToken);

        if (!result.IsSuccess)
            return Results.BadRequest(new { error = result.Error });

        return Results.Redirect(result.AuthorizationUrl!);
    }

    private static async Task<IResult> HandleCallbackAsync(
        string provider,
        string? code,
        string? state,
        HandleOAuthCallback handleOAuthCallback,
        HttpContext httpContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Results.Redirect("/?error=invalid_state");

        if (string.IsNullOrWhiteSpace(code))
            return Results.Redirect($"{state}?error=missing_code");

        var redirectUri = BuildRedirectUri(httpContext, configuration, provider);
        var result = await handleOAuthCallback.ExecuteAsync(provider, code, redirectUri, cancellationToken);

        var returnUrl = state;

        if (!result.IsSuccess)
        {
            if (result.Error == "access_denied")
                return Results.Redirect(returnUrl);

            return Results.Redirect($"{returnUrl}?error={result.Error}");
        }

        var isProduction = !httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        httpContext.Response.Cookies.Append(SessionCookieName, result.SessionId!.Value.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/",
            Domain = isProduction ? ".theaugmentedcraftsman.christianborrello.dev" : null
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

        var isProductionSignOut = !httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        httpContext.Response.Cookies.Delete(SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProductionSignOut,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Domain = isProductionSignOut ? ".theaugmentedcraftsman.christianborrello.dev" : null
        });

        return Results.NoContent();
    }

    private static string BuildRedirectUri(HttpContext httpContext, IConfiguration configuration, string provider)
    {
        var baseUrl = configuration["OAuth:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl))
            return $"{baseUrl.TrimEnd('/')}/api/auth/oauth/{provider}/callback";

        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host}/api/auth/oauth/{provider}/callback";
    }
}
