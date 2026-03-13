using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TacBlog.Infrastructure.Identity;
using Xunit;

namespace TacBlog.Application.Tests.Features.OAuth;

public class OAuthSettingsValidatorShould
{
    private readonly ILogger<OAuthSettingsValidator> _logger = Substitute.For<ILogger<OAuthSettingsValidator>>();
    private readonly OAuthSettingsValidator _validator;

    public OAuthSettingsValidatorShould()
    {
        _validator = new OAuthSettingsValidator(_logger);
    }

    [Fact]
    public void throw_when_github_client_id_is_missing_in_production()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "",
            GitHubClientSecret: "secret",
            GoogleClientId: null,
            GoogleClientSecret: null);

        var action = () => _validator.Validate(settings, isProduction: true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*GitHub:ClientId*required*");
    }

    [Fact]
    public void throw_when_github_client_secret_is_missing_in_production()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "client-id",
            GitHubClientSecret: "",
            GoogleClientId: null,
            GoogleClientSecret: null);

        var action = () => _validator.Validate(settings, isProduction: true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*GitHub:ClientSecret*required*");
    }

    [Fact]
    public void not_throw_when_github_credentials_are_complete_in_production()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "client-id",
            GitHubClientSecret: "client-secret",
            GoogleClientId: null,
            GoogleClientSecret: null);

        var action = () => _validator.Validate(settings, isProduction: true);

        action.Should().NotThrow();
    }

    [Fact]
    public void not_throw_when_github_credentials_are_missing_in_development()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "",
            GitHubClientSecret: "",
            GoogleClientId: null,
            GoogleClientSecret: null);

        var action = () => _validator.Validate(settings, isProduction: false);

        action.Should().NotThrow();
    }

    [Fact]
    public void log_warning_when_google_client_id_is_provided_but_secret_is_missing()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "github-id",
            GitHubClientSecret: "github-secret",
            GoogleClientId: "google-id",
            GoogleClientSecret: null);

        _validator.Validate(settings, isProduction: true);

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(m => m.ToString()!.Contains("Google OAuth")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void log_warning_when_google_client_secret_is_provided_but_id_is_missing()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "github-id",
            GitHubClientSecret: "github-secret",
            GoogleClientId: null,
            GoogleClientSecret: "google-secret");

        _validator.Validate(settings, isProduction: true);

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(m => m.ToString()!.Contains("Google OAuth")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void not_log_warning_when_google_is_fully_configured()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "github-id",
            GitHubClientSecret: "github-secret",
            GoogleClientId: "google-id",
            GoogleClientSecret: "google-secret");

        _validator.Validate(settings, isProduction: true);

        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(m => m.ToString()!.Contains("Google OAuth")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void not_log_warning_when_google_is_not_configured_at_all()
    {
        var settings = new OAuthSettings(
            GitHubClientId: "github-id",
            GitHubClientSecret: "github-secret",
            GoogleClientId: null,
            GoogleClientSecret: null);

        _validator.Validate(settings, isProduction: true);

        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(m => m.ToString()!.Contains("Google OAuth")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
