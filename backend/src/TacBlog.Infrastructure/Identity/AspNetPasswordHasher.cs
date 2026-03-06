using Microsoft.AspNetCore.Identity;
using TacBlog.Application.Ports.Driven;

namespace TacBlog.Infrastructure.Identity;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(string.Empty, password);

    public bool Verify(string password, string hashedPassword) =>
        _hasher.VerifyHashedPassword(string.Empty, hashedPassword, password)
            != PasswordVerificationResult.Failed;
}
