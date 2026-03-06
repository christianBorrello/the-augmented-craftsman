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

public sealed class LoginHandler(
    AdminCredentials adminCredentials,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IClock clock)
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaxFailedAttempts = 5;

    private readonly List<DateTime> _failureTimestamps = [];

    public void ResetFailedAttempts() => _failureTimestamps.Clear();

    public Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        if (IsLockedOut(now))
            return Task.FromResult(LoginResult.Lockout());

        if (!string.Equals(command.Email, adminCredentials.Email, StringComparison.OrdinalIgnoreCase))
        {
            RecordFailure(now);
            return Task.FromResult(LoginResult.Failure("Invalid email or password"));
        }

        if (!passwordHasher.Verify(command.Password, adminCredentials.HashedPassword))
        {
            RecordFailure(now);
            return Task.FromResult(LoginResult.Failure("Invalid email or password"));
        }

        _failureTimestamps.Clear();

        var token = tokenGenerator.Generate(command.Email);
        var expiresAt = now.AddHours(1);

        return Task.FromResult(LoginResult.Success(token, expiresAt));
    }

    private bool IsLockedOut(DateTime now)
    {
        PruneExpiredFailures(now);

        if (_failureTimestamps.Count < MaxFailedAttempts)
            return false;

        var mostRecentFailure = _failureTimestamps[^1];
        return now - mostRecentFailure < LockoutDuration;
    }

    private void RecordFailure(DateTime now)
    {
        _failureTimestamps.Add(now);
        PruneExpiredFailures(now);
    }

    private void PruneExpiredFailures(DateTime now)
    {
        _failureTimestamps.RemoveAll(t => now - t > FailureWindow);
    }
}
