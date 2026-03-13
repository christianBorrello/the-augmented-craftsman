using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.OAuth;

public sealed record HandleOAuthCallbackResult(
    bool IsSuccess,
    Guid? SessionId,
    string? Error)
{
    public static HandleOAuthCallbackResult Success(Guid sessionId) =>
        new(true, sessionId, null);

    public static HandleOAuthCallbackResult Failed(string error) =>
        new(false, null, error);
}

public sealed class HandleOAuthCallback(
    IOAuthClient oAuthClient,
    IReaderSessionRepository sessionRepository,
    IClock clock)
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromDays(30);

    public async Task<HandleOAuthCallbackResult> ExecuteAsync(
        string provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseProvider(provider, out var authProvider))
            return HandleOAuthCallbackResult.Failed("Unsupported provider");

        var tokenResult = await oAuthClient.ExchangeCodeAsync(
            authProvider, code, redirectUri, cancellationToken);

        if (!tokenResult.IsSuccess)
            return HandleOAuthCallbackResult.Failed(tokenResult.Error ?? "Token exchange failed");

        var profileResult = await oAuthClient.GetUserProfileAsync(
            authProvider, tokenResult.AccessToken!, cancellationToken);

        if (!profileResult.IsSuccess)
            return HandleOAuthCallbackResult.Failed(profileResult.Error ?? "Failed to get user profile");

        var profile = profileResult.Profile!;

        var now = clock.UtcNow;
        var session = ReaderSession.Create(
            profile.DisplayName,
            profile.AvatarUrl,
            authProvider,
            profile.ProviderId,
            now,
            now.Add(SessionDuration));

        await sessionRepository.SaveAsync(session, cancellationToken);

        return HandleOAuthCallbackResult.Success(session.Id);
    }

    private static bool TryParseProvider(string provider, out AuthProvider result) =>
        Enum.TryParse(provider, ignoreCase: true, out result) && result != AuthProvider.Unknown;
}
