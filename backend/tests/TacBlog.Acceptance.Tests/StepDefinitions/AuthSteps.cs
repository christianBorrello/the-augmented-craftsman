using FluentAssertions;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class AuthSteps
{
    private readonly AuthApiDriver _authDriver;
    private readonly AuthContext _authContext;

    public AuthSteps(AuthApiDriver authDriver, AuthContext authContext)
    {
        _authDriver = authDriver;
        _authContext = authContext;
    }

    [When("Christian logs in with email {string} and password {string}")]
    public async Task WhenChristianLogsInWith(string email, string password)
    {
        await _authDriver.Login(email, password);
    }

    [Given("Christian has failed login {int} times in the last {int} minutes")]
    public async Task GivenChristianHasFailedLoginNTimes(int attempts, int minutes)
    {
        for (var i = 0; i < attempts; i++)
        {
            await _authDriver.Login("christian.borrello@live.it", "wrong-password");
        }
    }

    [Then("the response contains a valid authentication token")]
    public void ThenTheResponseContainsAValidAuthenticationToken()
    {
        _authContext.JwtToken.Should().NotBeNullOrWhiteSpace();
    }

    [Then("no authentication token is issued")]
    public void ThenNoAuthenticationTokenIsIssued()
    {
        _authContext.JwtToken.Should().BeNull();
    }
}
