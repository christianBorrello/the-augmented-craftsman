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
        throw new NotImplementedException();
    }
}
