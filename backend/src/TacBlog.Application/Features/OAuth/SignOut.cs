using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.OAuth;

public sealed class SignOut(IReaderSessionRepository sessionRepository)
{
    public async Task ExecuteAsync(
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId is null)
            return;

        await sessionRepository.DeleteAsync(sessionId.Value, cancellationToken);
    }
}
