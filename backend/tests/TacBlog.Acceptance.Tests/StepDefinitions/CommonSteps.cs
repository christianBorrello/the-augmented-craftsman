using System.Net;
using FluentAssertions;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class CommonSteps
{
    private readonly ApiContext _apiContext;
    private readonly AuthApiDriver _authDriver;

    public CommonSteps(ApiContext apiContext, AuthApiDriver authDriver)
    {
        _apiContext = apiContext;
        _authDriver = authDriver;
    }

    [Given("Christian is authenticated")]
    public async Task GivenChristianIsAuthenticated()
    {
        await _authDriver.Authenticate();
    }

    [Given("no authentication is provided")]
    public void GivenNoAuthenticationIsProvided()
    {
        // No-op: AuthContext starts without a token
    }

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int expectedStatus)
    {
        _apiContext.StatusCode.Should().Be((HttpStatusCode)expectedStatus);
    }

    [Then("the response contains {string}")]
    public void ThenTheResponseContains(string expected)
    {
        _apiContext.LastResponseBody.Should().Contain(expected);
    }
}
