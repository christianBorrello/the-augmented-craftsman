using System.Net.Http.Json;
using System.Text.Json;
using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Identity;

public sealed class ProductionOAuthClient : IOAuthClient
{
    private readonly OAuthSettings _settings;
    private readonly HttpClient _httpClient;

    private const string GitHubAuthUrl = "https://github.com/login/oauth/authorize";
    private const string GitHubTokenUrl = "https://github.com/login/oauth/access_token";
    private const string GitHubUserUrl = "https://api.github.com/user";

    private const string GoogleAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
    private const string GoogleUserUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

    public ProductionOAuthClient(OAuthSettings settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "The-Augmented-Craftsman-OAuth");
    }

    public Task<AuthorizationUrlResult> GetAuthorizationUrlAsync(
        AuthProvider provider,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (provider == AuthProvider.Google)
        {
            if (string.IsNullOrEmpty(_settings.GoogleClientId) || string.IsNullOrEmpty(_settings.GoogleClientSecret))
                return Task.FromResult(AuthorizationUrlResult.Failure("Google OAuth not configured"));

            return Task.FromResult(AuthorizationUrlResult.Success(
                BuildGoogleAuthUrl(state, redirectUri, _settings.GoogleClientId)));
        }

        if (provider == AuthProvider.GitHub)
            return Task.FromResult(AuthorizationUrlResult.Success(BuildGitHubAuthUrl(state, redirectUri)));

        return Task.FromResult(AuthorizationUrlResult.Failure("Provider not supported"));
    }

    public async Task<OAuthTokenResult> ExchangeCodeAsync(
        AuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (provider == AuthProvider.GitHub)
        {
            return await ExchangeGitHubCodeAsync(code, redirectUri, cancellationToken);
        }

        if (provider == AuthProvider.Google)
        {
            return await ExchangeGoogleCodeAsync(code, redirectUri, cancellationToken);
        }

        return new OAuthTokenResult(false, null, "Unknown provider");
    }

    public Task<UserProfileResult> GetUserProfileAsync(
        AuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (provider != AuthProvider.GitHub && provider != AuthProvider.Google)
        {
            return Task.FromResult(UserProfileResult.Failure("Provider not supported"));
        }

        if (provider == AuthProvider.GitHub)
        {
            return GetGitHubUserProfileAsync(accessToken, cancellationToken);
        }

        return GetGoogleUserProfileAsync(accessToken, cancellationToken);
    }

    private string BuildGitHubAuthUrl(string state, string redirectUri)
    {
        return $"{GitHubAuthUrl}?client_id={_settings.GitHubClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=read:user&state={Uri.EscapeDataString(state)}";
    }

    private string BuildGoogleAuthUrl(string state, string redirectUri, string clientId)
    {
        return $"{GoogleAuthUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=openid%20email%20profile&state={Uri.EscapeDataString(state)}&response_type=code";
    }

    private async Task<OAuthTokenResult> ExchangeGitHubCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GitHubTokenUrl);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _settings.GitHubClientId,
            ["client_secret"] = _settings.GitHubClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new OAuthTokenResult(false, null, $"GitHub token endpoint returned {(int)response.StatusCode}");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (content.TryGetProperty("access_token", out var accessToken))
        {
            return new OAuthTokenResult(true, accessToken.GetString(), null);
        }

        var error = content.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
        return new OAuthTokenResult(false, null, error);
    }

    private async Task<OAuthTokenResult> ExchangeGoogleCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GoogleTokenUrl);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _settings.GoogleClientId!,
            ["client_secret"] = _settings.GoogleClientSecret!,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new OAuthTokenResult(false, null, $"Google token endpoint returned {(int)response.StatusCode}");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (content.TryGetProperty("access_token", out var accessToken))
        {
            return new OAuthTokenResult(true, accessToken.GetString(), null);
        }

        var error = content.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
        return new OAuthTokenResult(false, null, error);
    }

    private async Task<UserProfileResult> GetGitHubUserProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GitHubUserUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return UserProfileResult.Failure($"GitHub user endpoint returned {(int)response.StatusCode}");

        var user = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var id = user.GetProperty("id").GetInt64().ToString();
        var login = user.GetProperty("login").GetString() ?? "unknown";
        var name = user.TryGetProperty("name", out var n) ? n.GetString() ?? login : login;
        var avatar = user.GetProperty("avatar_url").GetString();

        return UserProfileResult.Success(new OAuthUserProfile(name, avatar, id));
    }

    private async Task<UserProfileResult> GetGoogleUserProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GoogleUserUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return UserProfileResult.Failure($"Google user endpoint returned {(int)response.StatusCode}");

        var user = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var id = user.GetProperty("id").GetString() ?? "unknown";
        var name = user.GetProperty("name").GetString() ?? user.GetProperty("email").GetString() ?? "unknown";
        var avatar = user.TryGetProperty("picture", out var p) ? p.GetString() : null;

        return UserProfileResult.Success(new OAuthUserProfile(name, avatar, id));
    }
}
