using TacBlog.Application.Features.OAuth;
using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface IOAuthClient
{
    Task<OAuthTokenResult> ExchangeCodeAsync(
        AuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<OAuthUserProfile> GetUserProfileAsync(
        AuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default);
}
