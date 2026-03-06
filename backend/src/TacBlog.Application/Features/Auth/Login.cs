using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResult(
    bool IsSuccess,
    bool IsLockedOut,
    string? Token,
    DateTime? ExpiresAt,
    string? ErrorMessage)
{
    public static LoginResult Success(string token, DateTime expiresAt) =>
        new(true, false, token, expiresAt, null);

    public static LoginResult Failure(string errorMessage) =>
        new(false, false, null, null, errorMessage);

    public static LoginResult Lockout() =>
        new(false, true, null, null, "Too many attempts. Try again in 15 minutes.");
}

public sealed record AdminCredentials(string Email, string HashedPassword);

public sealed class FailureTracker
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaxFailedAttempts = 5;

    private readonly List<DateTime> _timestamps = [];

    public void Record(DateTime now)
    {
        _timestamps.Add(now);
        PruneExpired(now);
    }

    public void Reset() => _timestamps.Clear();

    public bool IsLockedOut(DateTime now)
    {
        PruneExpired(now);

        if (_timestamps.Count < MaxFailedAttempts)
            return false;

        var mostRecentFailure = _timestamps[^1];
        return now - mostRecentFailure < LockoutDuration;
    }

    private void PruneExpired(DateTime now)
    {
        _timestamps.RemoveAll(t => now - t > FailureWindow);
    }
}

public sealed class LoginHandler(
    AdminCredentials adminCredentials,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IClock clock)
{
    private readonly FailureTracker _failureTracker = new();

    public void ResetFailedAttempts() => _failureTracker.Reset();

    public Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        if (_failureTracker.IsLockedOut(now))
            return Task.FromResult(LoginResult.Lockout());

        if (!AreCredentialsValid(command))
        {
            _failureTracker.Record(now);

            if (_failureTracker.IsLockedOut(now))
                return Task.FromResult(LoginResult.Lockout());

            return Task.FromResult(LoginResult.Failure("Invalid email or password"));
        }

        _failureTracker.Reset();

        var token = tokenGenerator.Generate(command.Email);
        var expiresAt = now.AddHours(1);

        return Task.FromResult(LoginResult.Success(token, expiresAt));
    }

    private bool AreCredentialsValid(LoginCommand command)
    {
        if (!string.Equals(command.Email, adminCredentials.Email, StringComparison.OrdinalIgnoreCase))
            return false;

        return passwordHasher.Verify(command.Password, adminCredentials.HashedPassword);
    }
}
