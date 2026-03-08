using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.OAuth;

public class CheckSessionShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IReaderSessionRepository _sessionRepository = Substitute.For<IReaderSessionRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CheckSession _useCase;

    public CheckSessionShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new CheckSession(_sessionRepository, _clock);
    }

    [Fact]
    public async Task return_authenticated_when_session_exists_and_not_expired()
    {
        var sessionId = Guid.NewGuid();
        var session = ReaderSession.Create(
            "Tomasz Kowalski",
            null,
            AuthProvider.GitHub,
            "github-123",
            FixedNow.AddHours(-1),
            FixedNow.AddDays(30));

        _sessionRepository.FindByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _useCase.ExecuteAsync(sessionId);

        result.IsAuthenticated.Should().BeTrue();
        result.DisplayName.Should().Be("Tomasz Kowalski");
        result.Provider.Should().Be("GitHub");
    }

    [Fact]
    public async Task return_not_authenticated_when_no_session_id()
    {
        var result = await _useCase.ExecuteAsync(null);

        result.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task return_not_authenticated_when_session_not_found()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepository.FindByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((ReaderSession?)null);

        var result = await _useCase.ExecuteAsync(sessionId);

        result.IsAuthenticated.Should().BeFalse();
    }
}
