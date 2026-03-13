using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.OAuth;

public sealed record InitiateOAuthResult(bool IsSuccess, string? AuthorizationUrl, string? Error)
{
    public static InitiateOAuthResult Success(string authorizationUrl) =>
        new(true, authorizationUrl, null);

    public static InitiateOAuthResult UnsupportedProvider() =>
        new(false, null, "Unsupported provider");

    public static InitiateOAuthResult Failure(string error) =>
        new(false, null, error);
}

public sealed class InitiateOAuth(IOAuthClient oAuthClient)
{
    public async Task<InitiateOAuthResult> ExecuteAsync(
        string provider,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AuthProvider>(provider, ignoreCase: true, out var authProvider) || authProvider == AuthProvider.Unknown)
            return InitiateOAuthResult.UnsupportedProvider();

        var result = await oAuthClient.GetAuthorizationUrlAsync(
            authProvider, state, redirectUri, cancellationToken);

        if (!result.IsSuccess)
            return InitiateOAuthResult.Failure(result.Error ?? "Failed to get authorization URL");

        return InitiateOAuthResult.Success(result.AuthorizationUrl!);
    }
}
