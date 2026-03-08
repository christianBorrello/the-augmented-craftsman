using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.OAuth;

public class HandleOAuthCallbackShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IOAuthClient _oAuthClient = Substitute.For<IOAuthClient>();
    private readonly IReaderSessionRepository _sessionRepository = Substitute.For<IReaderSessionRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly HandleOAuthCallback _useCase;

    public HandleOAuthCallbackShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new HandleOAuthCallback(_oAuthClient, _sessionRepository, _clock);
    }

    [Theory]
    [InlineData("github", AuthProvider.GitHub, "octocat-123")]
    [InlineData("google", AuthProvider.Google, "google-456")]
    public async Task create_session_when_code_exchange_succeeds(
        string providerName,
        AuthProvider expectedProvider,
        string providerId)
    {
        _oAuthClient.ExchangeCodeAsync(expectedProvider, "valid-code", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OAuthTokenResult(true, "access-token", null));
        _oAuthClient.GetUserProfileAsync(expectedProvider, "access-token", Arg.Any<CancellationToken>())
            .Returns(new OAuthUserProfile("Test User", "https://avatar.url", providerId));

        var result = await _useCase.ExecuteAsync(providerName, "valid-code", "https://localhost/callback");

        result.IsSuccess.Should().BeTrue();
        result.SessionId.Should().NotBeNull();
        await _sessionRepository.Received(1).SaveAsync(
            Arg.Is<ReaderSession>(s =>
                s.DisplayName == "Test User"
                && s.Provider == expectedProvider
                && s.ProviderId == providerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_error_when_code_exchange_fails()
    {
        _oAuthClient.ExchangeCodeAsync(AuthProvider.GitHub, "bad-code", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new OAuthTokenResult(false, null, "access_denied"));

        var result = await _useCase.ExecuteAsync("github", "bad-code", "https://localhost/callback");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("access_denied");
        await _sessionRepository.DidNotReceive().SaveAsync(Arg.Any<ReaderSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_error_for_unsupported_provider()
    {
        var result = await _useCase.ExecuteAsync("twitter", "code", "https://localhost/callback");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unsupported");
    }
}
