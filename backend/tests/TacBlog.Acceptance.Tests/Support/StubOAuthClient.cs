using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Acceptance.Tests.Support;

public sealed class StubOAuthClient : IOAuthClient
{
    private string _displayName = "Test User";
    private string? _avatarUrl;
    private string _providerId = "stub-provider-id";
    private OAuthBehavior _behavior = OAuthBehavior.ConsentGranted;

    public void ConfigureConsentGranted(string displayName, string? avatarUrl = null, string providerId = "stub-id")
    {
        _behavior = OAuthBehavior.ConsentGranted;
        _displayName = displayName;
        _avatarUrl = avatarUrl;
        _providerId = providerId;
    }

    public void ConfigureConsentDenied()
    {
        _behavior = OAuthBehavior.ConsentDenied;
    }

    public void ConfigureProviderError()
    {
        _behavior = OAuthBehavior.ProviderError;
    }

    public void Reset()
    {
        _behavior = OAuthBehavior.ConsentGranted;
        _displayName = "Test User";
        _avatarUrl = null;
        _providerId = "stub-provider-id";
    }

    public Task<OAuthTokenResult> ExchangeCodeAsync(
        AuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        return _behavior switch
        {
            OAuthBehavior.ConsentGranted => Task.FromResult(
                new OAuthTokenResult(true, "stub-access-token", null)),
            OAuthBehavior.ConsentDenied => Task.FromResult(
                new OAuthTokenResult(false, null, "access_denied")),
            OAuthBehavior.ProviderError => Task.FromResult(
                new OAuthTokenResult(false, null, "server_error")),
            _ => throw new InvalidOperationException($"Unknown behavior: {_behavior}")
        };
    }

    public Task<OAuthUserProfile> GetUserProfileAsync(
        AuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new OAuthUserProfile(_displayName, _avatarUrl, _providerId));
    }

    private enum OAuthBehavior
    {
        ConsentGranted,
        ConsentDenied,
        ProviderError
    }
}
