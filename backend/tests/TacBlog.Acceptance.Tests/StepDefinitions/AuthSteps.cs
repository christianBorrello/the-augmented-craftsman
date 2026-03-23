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

    [Given("Christian provides the correct API key")]
    public void GivenChristianProvidesTheCorrectApiKey()
    {
        _authDriver.Authenticate();
    }

    [When("Christian provides a wrong API key")]
    public void WhenChristianProvidesAWrongApiKey()
    {
        _authContext.ApiKey = "wrong-api-key";
    }

    [Then("Christian is authenticated as admin")]
    public void ThenChristianIsAuthenticatedAsAdmin()
    {
        _authContext.IsAuthenticated.Should().BeTrue();
    }
}
