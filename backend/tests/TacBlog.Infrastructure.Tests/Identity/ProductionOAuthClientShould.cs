using FluentAssertions;
using TacBlog.Application.Features.OAuth;
using TacBlog.Domain;
using TacBlog.Infrastructure.Identity;
using Xunit;

namespace TacBlog.Infrastructure.Tests.Identity;

public class ProductionOAuthClientShould
{
    private readonly OAuthSettings _settingsWithGoogle = new(
        GitHubClientId: "github-id",
        GitHubClientSecret: "github-secret",
        GoogleClientId: "google-id",
        GoogleClientSecret: "google-secret");

    private readonly OAuthSettings _settingsWithoutGoogle = new(
        GitHubClientId: "github-id",
        GitHubClientSecret: "github-secret",
        GoogleClientId: null,
        GoogleClientSecret: null);

    [Fact]
    public async Task return_failure_for_unsupported_provider_in_get_authorization_url()
    {
        var client = new ProductionOAuthClient(_settingsWithGoogle, new HttpClient());

        var result = await client.GetAuthorizationUrlAsync(
            AuthProvider.Unknown, "state", "http://localhost/callback");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not supported");
    }

    [Fact]
    public async Task return_failure_for_missing_google_credentials()
    {
        var client = new ProductionOAuthClient(_settingsWithoutGoogle, new HttpClient());

        var result = await client.GetAuthorizationUrlAsync(
            AuthProvider.Google, "state", "http://localhost/callback");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Google");
        result.Error.Should().Contain("configured");
    }

    [Fact]
    public async Task return_failure_for_unsupported_provider_in_get_user_profile()
    {
        var client = new ProductionOAuthClient(_settingsWithGoogle, new HttpClient());

        var result = await client.GetUserProfileAsync(
            AuthProvider.Unknown, "access-token");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not supported");
    }

    [Fact]
    public async Task return_authorization_url_for_github()
    {
        var client = new ProductionOAuthClient(_settingsWithGoogle, new HttpClient());

        var result = await client.GetAuthorizationUrlAsync(
            AuthProvider.GitHub, "state", "http://localhost/callback");

        result.IsSuccess.Should().BeTrue();
        result.AuthorizationUrl.Should().Contain("github.com");
    }

    [Fact]
    public async Task return_authorization_url_for_google()
    {
        var client = new ProductionOAuthClient(_settingsWithGoogle, new HttpClient());

        var result = await client.GetAuthorizationUrlAsync(
            AuthProvider.Google, "state", "http://localhost/callback");

        result.IsSuccess.Should().BeTrue();
        result.AuthorizationUrl.Should().Contain("accounts.google.com");
    }
}
