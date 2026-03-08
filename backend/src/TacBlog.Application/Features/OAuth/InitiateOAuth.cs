using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.OAuth;

public sealed record InitiateOAuthResult(bool IsSuccess, string? AuthorizationUrl, string? Error)
{
    public static InitiateOAuthResult Success(string authorizationUrl) =>
        new(true, authorizationUrl, null);

    public static InitiateOAuthResult UnsupportedProvider() =>
        new(false, null, "Unsupported provider");
}

public sealed class InitiateOAuth(IOAuthClient oAuthClient)
{
    public async Task<InitiateOAuthResult> ExecuteAsync(
        string provider,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AuthProvider>(provider, ignoreCase: true, out var authProvider))
            return InitiateOAuthResult.UnsupportedProvider();

        var authorizationUrl = await oAuthClient.GetAuthorizationUrlAsync(
            authProvider, state, redirectUri, cancellationToken);

        return InitiateOAuthResult.Success(authorizationUrl);
    }
}
