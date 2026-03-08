using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.OAuth;

public sealed record CheckSessionResult(
    bool IsAuthenticated,
    string? DisplayName,
    string? AvatarUrl,
    string? Provider)
{
    public static CheckSessionResult Authenticated(
        string displayName,
        string? avatarUrl,
        AuthProvider provider) =>
        new(true, displayName, avatarUrl, provider.ToString());

    public static CheckSessionResult NotAuthenticated() =>
        new(false, null, null, null);
}

public sealed class CheckSession(
    IReaderSessionRepository sessionRepository,
    IClock clock)
{
    public async Task<CheckSessionResult> ExecuteAsync(
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId is null)
            return CheckSessionResult.NotAuthenticated();

        var session = await sessionRepository.FindByIdAsync(sessionId.Value, cancellationToken);

        if (session is null || session.IsExpired(clock.UtcNow))
            return CheckSessionResult.NotAuthenticated();

        return CheckSessionResult.Authenticated(
            session.DisplayName,
            session.AvatarUrl,
            session.Provider);
    }
}
