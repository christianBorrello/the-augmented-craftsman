using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;
using TacBlog.Acceptance.Tests.Support;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class OAuthSteps(
    OAuthApiDriver oAuthDriver,
    ApiContext apiContext,
    ReaderSessionContext sessionContext,
    TacBlogWebApplicationFactory factory)
{
    private HttpResponseMessage? _callbackResponse;

    [Given("a reader has granted consent on GitHub as {string}")]
    public void GivenAReaderHasGrantedConsentOnGitHubAs(string displayName)
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureConsentGranted(displayName, null, $"github-{displayName.ToLowerInvariant().Replace(' ', '-')}");
    }

    [Given("a reader has granted consent on Google as {string}")]
    public void GivenAReaderHasGrantedConsentOnGoogleAs(string displayName)
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureConsentGranted(displayName, null, $"google-{displayName.ToLowerInvariant().Replace(' ', '-')}");
    }

    [Given("a reader session exists for {string} via {string}")]
    public async Task GivenAReaderSessionExistsForVia(string displayName, string providerName)
    {
        var provider = Enum.Parse<AuthProvider>(providerName, ignoreCase: true);
        var session = ReaderSession.Create(
            displayName,
            null,
            provider,
            $"{providerName.ToLowerInvariant()}-{displayName.ToLowerInvariant().Replace(' ', '-')}",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30));

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReaderSessionRepository>();
        await repository.SaveAsync(session, CancellationToken.None);

        sessionContext.SessionCookie = session.Id.ToString();
    }

    [When("the OAuth callback is received with a valid authorization code for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithAValidAuthorizationCodeFor(string provider)
    {
        await oAuthDriver.SimulateCallback(provider);
        _callbackResponse = apiContext.LastResponse;
    }

    [When("the reader checks their session status")]
    public async Task WhenTheReaderChecksTheirSessionStatus()
    {
        await oAuthDriver.CheckSession();
    }

    [Then("a reader session is created")]
    public void ThenAReaderSessionIsCreated()
    {
        sessionContext.SessionCookie.Should().NotBeNullOrEmpty("a session cookie should be set after OAuth callback");
    }

    [Then("the session contains display name {string}")]
    public async Task ThenTheSessionContainsDisplayName(string expectedName)
    {
        await oAuthDriver.CheckSession();
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("displayName").GetString()
            .Should().Be(expectedName);
    }

    [Then("the session contains provider {string}")]
    public void ThenTheSessionContainsProvider(string expectedProvider)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("provider").GetString()
            .Should().Be(expectedProvider);
    }

    [Then("the reader is redirected back to the original post")]
    public void ThenTheReaderIsRedirectedBackToTheOriginalPost()
    {
        _callbackResponse.Should().NotBeNull("a callback response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Then("the session status indicates authenticated")]
    public void ThenTheSessionStatusIndicatesAuthenticated()
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("authenticated").GetBoolean()
            .Should().BeTrue();
    }

    [Then("the response contains display name {string}")]
    public void ThenTheResponseContainsDisplayName(string expectedName)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("displayName").GetString()
            .Should().Be(expectedName);
    }

    [Then("the response contains provider {string}")]
    public void ThenTheResponseContainsProvider(string expectedProvider)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("provider").GetString()
            .Should().Be(expectedProvider);
    }
}
