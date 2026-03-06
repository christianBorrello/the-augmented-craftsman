using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResult(bool IsSuccess, string? Token, DateTime? ExpiresAt, string? ErrorMessage)
{
    public static LoginResult Success(string token, DateTime expiresAt) =>
        new(true, token, expiresAt, null);

    public static LoginResult Failure(string errorMessage) =>
        new(false, null, null, errorMessage);
}

public sealed record AdminCredentials(string Email, string HashedPassword);

public sealed class LoginHandler(
    AdminCredentials adminCredentials,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator)
{
    public Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(command.Email, adminCredentials.Email, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(LoginResult.Failure("Invalid email or password"));

        if (!passwordHasher.Verify(command.Password, adminCredentials.HashedPassword))
            return Task.FromResult(LoginResult.Failure("Invalid email or password"));

        var token = tokenGenerator.Generate(command.Email);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return Task.FromResult(LoginResult.Success(token, expiresAt));
    }
}
