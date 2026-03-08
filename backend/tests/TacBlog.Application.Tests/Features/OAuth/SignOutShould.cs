using NSubstitute;
using TacBlog.Application.Features.OAuth;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.OAuth;

public class SignOutShould
{
    private readonly IReaderSessionRepository _sessionRepository = Substitute.For<IReaderSessionRepository>();
    private readonly SignOut _useCase;

    public SignOutShould()
    {
        _useCase = new SignOut(_sessionRepository);
    }

    [Fact]
    public async Task delete_session_when_it_exists()
    {
        var sessionId = Guid.NewGuid();

        await _useCase.ExecuteAsync(sessionId);

        await _sessionRepository.Received(1).DeleteAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task not_throw_when_session_does_not_exist()
    {
        var sessionId = Guid.NewGuid();

        await _useCase.ExecuteAsync(sessionId);

        await _sessionRepository.Received(1).DeleteAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task handle_null_session_id()
    {
        await _useCase.ExecuteAsync(null);

        await _sessionRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
