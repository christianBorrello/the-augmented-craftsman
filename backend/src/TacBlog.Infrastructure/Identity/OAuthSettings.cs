namespace TacBlog.Infrastructure.Identity;

public sealed record OAuthSettings(
    string GitHubClientId,
    string GitHubClientSecret,
    string? GoogleClientId,
    string? GoogleClientSecret
);
