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

    [When("a reader initiates sign-in with {string} for post {string}")]
    public async Task WhenAReaderInitiatesSignInWithForPost(string provider, string postSlug)
    {
        await oAuthDriver.InitiateOAuth(provider, $"/blog/{postSlug}");
        _callbackResponse = apiContext.LastResponse;
    }

    [When("the OAuth callback is received with a valid authorization code for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithAValidAuthorizationCodeFor(string provider)
    {
        await oAuthDriver.SimulateCallback(provider);
        _callbackResponse = apiContext.LastResponse;
    }

    [Given("a reader session exists for {string} via {string} that has expired")]
    public async Task GivenAReaderSessionExistsForViaThatHasExpired(string displayName, string providerName)
    {
        var provider = Enum.Parse<AuthProvider>(providerName, ignoreCase: true);
        var session = ReaderSession.Create(
            displayName,
            null,
            provider,
            $"{providerName.ToLowerInvariant()}-{displayName.ToLowerInvariant().Replace(' ', '-')}",
            DateTime.UtcNow.AddDays(-31),
            DateTime.UtcNow.AddDays(-1));

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReaderSessionRepository>();
        await repository.SaveAsync(session, CancellationToken.None);

        sessionContext.SessionCookie = session.Id.ToString();
    }

    [When("a reader with no session checks their session status")]
    public async Task WhenAReaderWithNoSessionChecksTheirSessionStatus()
    {
        sessionContext.SessionCookie = null;
        await oAuthDriver.CheckSession();
    }

    [When("the OAuth callback is received with consent denied for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithConsentDeniedFor(string provider)
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureConsentDenied();
        await oAuthDriver.SimulateCallback(provider);
        _callbackResponse = apiContext.LastResponse;
    }

    [When("the OAuth callback is received with a provider error for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithAProviderErrorFor(string provider)
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureProviderError();
        await oAuthDriver.SimulateCallback(provider);
        _callbackResponse = apiContext.LastResponse;
    }

    [When("the OAuth callback is received with an invalid state parameter for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithAnInvalidStateParameterFor(string provider)
    {
        await oAuthDriver.SimulateCallback(provider, code: "test-code", state: "");
        _callbackResponse = apiContext.LastResponse;
    }

    [When("the reader checks their session status")]
    public async Task WhenTheReaderChecksTheirSessionStatus()
    {
        await oAuthDriver.CheckSession();
    }

    [When("the reader signs out")]
    public async Task WhenTheReaderSignsOut()
    {
        await oAuthDriver.SignOut();
    }

    [When("a reader with no session signs out")]
    public async Task WhenAReaderWithNoSessionSignsOut()
    {
        sessionContext.SessionCookie = null;
        await oAuthDriver.SignOut();
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

    [Then("no session is created")]
    public void ThenNoSessionIsCreated()
    {
        sessionContext.SessionCookie.Should().BeNull("no session should exist after error path");
    }

    [Then("the reader is redirected back to the original post with an error indicator")]
    public void ThenTheReaderIsRedirectedBackToTheOriginalPostWithAnErrorIndicator()
    {
        _callbackResponse.Should().NotBeNull("a callback response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = _callbackResponse.Headers.Location?.ToString();
        location.Should().NotBeNull();
        location.Should().Contain("error=");
    }

    [Then("the reader is redirected to the GitHub authorization page")]
    public void ThenTheReaderIsRedirectedToTheGitHubAuthorizationPage()
    {
        _callbackResponse.Should().NotBeNull("a redirect response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = _callbackResponse.Headers.Location?.ToString();
        location.Should().NotBeNull();
        location.Should().Contain("github.example.com/authorize");
    }

    [Then("the reader is redirected back to the original post")]
    public void ThenTheReaderIsRedirectedBackToTheOriginalPost()
    {
        _callbackResponse.Should().NotBeNull("a callback response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Then("the session is cleared")]
    public void ThenTheSessionIsCleared()
    {
        sessionContext.SessionCookie.Should().BeNull("session cookie should be cleared after sign out");
    }

    [Then("checking session status shows not authenticated")]
    public async Task ThenCheckingSessionStatusShowsNotAuthenticated()
    {
        await oAuthDriver.CheckSession();
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("authenticated").GetBoolean()
            .Should().BeFalse();
    }

    [Then("the session status indicates not authenticated")]
    public void ThenTheSessionStatusIndicatesNotAuthenticated()
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("authenticated").GetBoolean()
            .Should().BeFalse();
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
