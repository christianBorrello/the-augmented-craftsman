using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Auth;
using TacBlog.Application.Ports.Driven;
using Xunit;

namespace TacBlog.Application.Tests.Features.Auth;

public class LoginShould
{
    private const string AdminEmail = "admin@example.com";
    private const string AdminHashedPassword = "hashed-password-value";
    private const string ValidPassword = "correct-password";
    private const string GeneratedToken = "jwt-token-value";

    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly LoginHandler _handler;

    public LoginShould()
    {
        var adminCredentials = new AdminCredentials(AdminEmail, AdminHashedPassword);
        _handler = new LoginHandler(adminCredentials, _passwordHasher, _tokenGenerator);
    }

    [Fact]
    public async Task return_token_when_credentials_are_valid()
    {
        _passwordHasher.Verify(ValidPassword, AdminHashedPassword).Returns(true);
        _tokenGenerator.Generate(AdminEmail).Returns(GeneratedToken);

        var result = await _handler.HandleAsync(new LoginCommand(AdminEmail, ValidPassword));

        result.IsSuccess.Should().BeTrue();
        result.Token.Should().Be(GeneratedToken);
        result.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task return_failure_when_password_is_wrong()
    {
        _passwordHasher.Verify("wrong-password", AdminHashedPassword).Returns(false);

        var result = await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong-password"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task return_failure_when_email_is_unknown()
    {
        var result = await _handler.HandleAsync(new LoginCommand("unknown@example.com", ValidPassword));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
        result.Token.Should().BeNull();
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }
}
