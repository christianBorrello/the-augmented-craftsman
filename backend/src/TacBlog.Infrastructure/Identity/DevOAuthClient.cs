using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Identity;

public sealed class DevOAuthClient : IOAuthClient
{
    public Task<string> GetAuthorizationUrlAsync(
        AuthProvider provider,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var callbackUrl = $"{redirectUri}?code=dev-fake-code&state={Uri.EscapeDataString(state)}";
        return Task.FromResult(callbackUrl);
    }

    public Task<OAuthTokenResult> ExchangeCodeAsync(
        AuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new OAuthTokenResult(true, "dev-access-token", null));
    }

    public Task<OAuthUserProfile> GetUserProfileAsync(
        AuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var (name, avatar) = provider switch
        {
            AuthProvider.GitHub => ("Dev User (GitHub)", "https://api.dicebear.com/7.x/thumbs/svg?seed=github"),
            AuthProvider.Google => ("Dev User (Google)", "https://api.dicebear.com/7.x/thumbs/svg?seed=google"),
            _ => ("Dev User", null)
        };

        return Task.FromResult(new OAuthUserProfile(name, avatar, $"dev-{provider.ToString().ToLowerInvariant()}-001"));
    }
}
