namespace TacBlog.Application.Features.OAuth;

public sealed record OAuthUserProfile(string DisplayName, string? AvatarUrl, string ProviderId);

public sealed record OAuthTokenResult(bool IsSuccess, string? AccessToken, string? Error);
