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
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly LoginHandler _handler;

    public LoginShould()
    {
        var adminCredentials = new AdminCredentials(AdminEmail, AdminHashedPassword);
        _clock.UtcNow.Returns(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        _handler = new LoginHandler(adminCredentials, _passwordHasher, _tokenGenerator, _clock);
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

    [Fact]
    public async Task return_lockout_error_after_5_failed_attempts_within_10_minutes()
    {
        _passwordHasher.Verify("wrong", AdminHashedPassword).Returns(false);
        var baseTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            _clock.UtcNow.Returns(baseTime.AddMinutes(i));
            await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));
        }

        _clock.UtcNow.Returns(baseTime.AddMinutes(6));
        var result = await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));

        result.IsLockedOut.Should().BeTrue();
        result.ErrorMessage.Should().Be("Too many attempts. Try again in 15 minutes.");
    }

    [Fact]
    public async Task allow_login_after_lockout_expires()
    {
        _passwordHasher.Verify("wrong", AdminHashedPassword).Returns(false);
        _passwordHasher.Verify(ValidPassword, AdminHashedPassword).Returns(true);
        _tokenGenerator.Generate(AdminEmail).Returns(GeneratedToken);
        var baseTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
        {
            _clock.UtcNow.Returns(baseTime.AddMinutes(i));
            await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));
        }

        _clock.UtcNow.Returns(baseTime.AddMinutes(20));
        var result = await _handler.HandleAsync(new LoginCommand(AdminEmail, ValidPassword));

        result.IsSuccess.Should().BeTrue();
        result.Token.Should().Be(GeneratedToken);
    }

    [Fact]
    public async Task reset_failure_counter_on_successful_login()
    {
        _passwordHasher.Verify("wrong", AdminHashedPassword).Returns(false);
        _passwordHasher.Verify(ValidPassword, AdminHashedPassword).Returns(true);
        _tokenGenerator.Generate(AdminEmail).Returns(GeneratedToken);
        var baseTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 4; i++)
        {
            _clock.UtcNow.Returns(baseTime.AddMinutes(i));
            await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));
        }

        _clock.UtcNow.Returns(baseTime.AddMinutes(5));
        await _handler.HandleAsync(new LoginCommand(AdminEmail, ValidPassword));

        for (var i = 0; i < 4; i++)
        {
            _clock.UtcNow.Returns(baseTime.AddMinutes(6 + i));
            await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));
        }

        _clock.UtcNow.Returns(baseTime.AddMinutes(10));
        var result = await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));

        result.IsLockedOut.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task not_count_failures_outside_10_minute_window()
    {
        _passwordHasher.Verify("wrong", AdminHashedPassword).Returns(false);
        var baseTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 3; i++)
        {
            _clock.UtcNow.Returns(baseTime.AddMinutes(i));
            await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));
        }

        _clock.UtcNow.Returns(baseTime.AddMinutes(15));
        for (var i = 0; i < 3; i++)
        {
            _clock.UtcNow.Returns(baseTime.AddMinutes(15 + i));
            await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));
        }

        _clock.UtcNow.Returns(baseTime.AddMinutes(18));
        var result = await _handler.HandleAsync(new LoginCommand(AdminEmail, "wrong"));

        result.IsLockedOut.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
    }
}
