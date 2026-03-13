namespace TacBlog.Application.Features.OAuth;

public sealed record OAuthUserProfile(string DisplayName, string? AvatarUrl, string ProviderId);

public sealed record OAuthTokenResult(bool IsSuccess, string? AccessToken, string? Error);

public sealed record AuthorizationUrlResult(bool IsSuccess, string? AuthorizationUrl, string? Error)
{
    public static AuthorizationUrlResult Success(string url) => new(true, url, null);
    public static AuthorizationUrlResult Failure(string error) => new(false, null, error);
}

public sealed record UserProfileResult(bool IsSuccess, OAuthUserProfile? Profile, string? Error)
{
    public static UserProfileResult Success(OAuthUserProfile profile) => new(true, profile, null);
    public static UserProfileResult Failure(string error) => new(false, null, error);
}
