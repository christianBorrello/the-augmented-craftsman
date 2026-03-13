using TacBlog.Application.Features.OAuth;
using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface IOAuthClient
{
    Task<AuthorizationUrlResult> GetAuthorizationUrlAsync(
        AuthProvider provider,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<OAuthTokenResult> ExchangeCodeAsync(
        AuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<UserProfileResult> GetUserProfileAsync(
        AuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default);
}
