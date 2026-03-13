using Microsoft.Extensions.Logging;

namespace TacBlog.Infrastructure.Identity;

public sealed class OAuthSettingsValidator
{
    private readonly ILogger<OAuthSettingsValidator> _logger;

    public OAuthSettingsValidator(ILogger<OAuthSettingsValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(OAuthSettings settings, bool isProduction)
    {
        ValidateGitHub(settings, isProduction);
        ValidateGoogle(settings);
    }

    private void ValidateGitHub(OAuthSettings settings, bool isProduction)
    {
        if (isProduction && string.IsNullOrEmpty(settings.GitHubClientId))
        {
            throw new InvalidOperationException(
                "OAuth:GitHub:ClientId is required in production. Set it via environment variable OAuth__GitHub__ClientId.");
        }

        if (isProduction && string.IsNullOrEmpty(settings.GitHubClientSecret))
        {
            throw new InvalidOperationException(
                "OAuth:GitHub:ClientSecret is required in production. Set it via environment variable OAuth__GitHub__ClientSecret.");
        }
    }

    private void ValidateGoogle(OAuthSettings settings)
    {
        var hasClientId = !string.IsNullOrEmpty(settings.GoogleClientId);
        var hasClientSecret = !string.IsNullOrEmpty(settings.GoogleClientSecret);

        // Google is optional - no configuration is fine
        if (!hasClientId && !hasClientSecret)
        {
            return;
        }

        // Partial configuration triggers warning
        if (hasClientId != hasClientSecret)
        {
            _logger.LogWarning(
                "Google OAuth is partially configured. For Google sign-in to work, both GoogleClientId and GoogleClientSecret must be set. " +
                "Set them via environment variables OAuth__Google__ClientId and OAuth__Google__ClientSecret.");
        }
    }
}
