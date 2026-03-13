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

    // Walking skeleton steps

    [Given("a valid authorization code is available for {string}")]
    public void GivenAValidAuthorizationCodeIsAvailableFor(string provider)
    {
        // Stub is ConsentGranted by default — no-op
    }

    [Given("an authenticated reader session exists")]
    public async Task GivenAnAuthenticatedReaderSessionExists()
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureConsentGranted("Test User");
        await oAuthDriver.SimulateCallback("github");
        _callbackResponse = apiContext.LastResponse;
    }

    [Then("the reader is redirected to the authorization page")]
    public void ThenTheReaderIsRedirectedToTheAuthorizationPage()
    {
        _callbackResponse.Should().NotBeNull("a redirect response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
        _callbackResponse.Headers.Location.Should().NotBeNull();
    }

    [Then("the reader session is valid")]
    public void ThenTheReaderSessionIsValid()
    {
        sessionContext.IsAuthenticated.Should().BeTrue("a session should have been created");
    }

    // Milestone 1: configuration

    [Then("the request is rejected with bad request error")]
    public void ThenTheRequestIsRejectedWithBadRequestError()
    {
        apiContext.LastResponse!.IsSuccessStatusCode.Should().BeFalse("the request should have been rejected");
    }

    [Then("the error indicates the provider is not supported")]
    public async Task ThenTheErrorIndicatesTheProviderIsNotSupported()
    {
        var body = await apiContext.LastResponse!.Content.ReadAsStringAsync();
        body.Should().ContainAny("Unsupported", "unsupported", "not supported");
    }

    [When("the OAuth callback is received without authorization code for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithoutAuthorizationCodeFor(string provider)
    {
        await oAuthDriver.SimulateCallbackWithoutCode(provider);
        _callbackResponse = apiContext.LastResponse;
    }

    [Then("the reader is redirected back with an error indicator")]
    public void ThenTheReaderIsRedirectedBackWithAnErrorIndicator()
    {
        _callbackResponse.Should().NotBeNull("a callback response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = _callbackResponse.Headers.Location?.ToString();
        location.Should().NotBeNull();
        location.Should().Contain("error=");
    }

    [When("the OAuth callback is received with empty state for {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithEmptyStateFor(string provider)
    {
        await oAuthDriver.SimulateCallback(provider, code: "test-code", state: "");
        _callbackResponse = apiContext.LastResponse;
    }

    [When("the OAuth callback is received for missing provider")]
    public async Task WhenTheOAuthCallbackIsReceivedForMissingProvider()
    {
        await oAuthDriver.SimulateCallbackForMissingProvider();
        _callbackResponse = apiContext.LastResponse;
    }

    // Milestone 2 & 3: GitHub/Google flow

    [Then("the authorization URL contains GitHub")]
    public void ThenTheAuthorizationUrlContainsGitHub()
    {
        var location = _callbackResponse?.Headers.Location?.ToString();
        location.Should().NotBeNull().And.Contain("github");
    }

    [Then("the authorization URL contains Google")]
    public void ThenTheAuthorizationUrlContainsGoogle()
    {
        var location = _callbackResponse?.Headers.Location?.ToString();
        location.Should().NotBeNull().And.Contain("google");
    }

    [When("the OAuth callback is received with a valid authorization code for {string} with return URL {string}")]
    public async Task WhenTheOAuthCallbackIsReceivedWithAValidAuthorizationCodeForWithReturnUrl(
        string provider,
        string returnUrl)
    {
        await oAuthDriver.SimulateCallback(provider, code: "test-code", state: returnUrl);
        _callbackResponse = apiContext.LastResponse;
    }

    [Then("the reader is redirected back to {string}")]
    public void ThenTheReaderIsRedirectedBackTo(string expectedPath)
    {
        _callbackResponse.Should().NotBeNull("a callback response should have been captured");
        _callbackResponse!.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = _callbackResponse.Headers.Location?.ToString();
        location.Should().NotBeNull();
        location.Should().StartWith(expectedPath);
    }

    [Given("a reader has granted consent on GitHub as {string} with avatar {string}")]
    public void GivenAReaderHasGrantedConsentOnGitHubAsWithAvatar(string displayName, string avatarUrl)
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureConsentGranted(displayName, avatarUrl, $"github-{displayName.ToLowerInvariant().Replace(' ', '-')}");
    }

    [Given("a reader has granted consent on Google as {string} with avatar {string}")]
    public void GivenAReaderHasGrantedConsentOnGoogleAsWithAvatar(string displayName, string avatarUrl)
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureConsentGranted(displayName, avatarUrl, $"google-{displayName.ToLowerInvariant().Replace(' ', '-')}");
    }

    [Then("the session contains avatar URL {string}")]
    public async Task ThenTheSessionContainsAvatarUrl(string expectedUrl)
    {
        await oAuthDriver.CheckSession();
        apiContext.LastResponseJson!.RootElement.GetProperty("avatarUrl").GetString()
            .Should().Be(expectedUrl);
    }

    // Milestone 4: error handling

    [Then("no error indicator is shown")]
    public void ThenNoErrorIndicatorIsShown()
    {
        _callbackResponse.Should().NotBeNull("a callback response should have been captured");
        var location = _callbackResponse!.Headers.Location?.ToString();
        location.Should().NotContain("error=");
    }

    [Given("the OAuth provider is configured to fail token exchange")]
    public void GivenTheOAuthProviderIsConfiguredToFailTokenExchange()
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureProviderError();
    }

    [Given("the OAuth provider is configured to fail user profile fetch")]
    public void GivenTheOAuthProviderIsConfiguredToFailUserProfileFetch()
    {
        var stub = factory.Services.GetRequiredService<StubOAuthClient>();
        stub.ConfigureProfileFetchError();
    }

    // Milestone 5: session management

    [Then("the operation succeeds")]
    public void ThenTheOperationSucceeds()
    {
        apiContext.LastResponse!.IsSuccessStatusCode.Should().BeTrue("the operation should have succeeded");
    }

    [When("the reader checks their session status again")]
    public async Task WhenTheReaderChecksTheirSessionStatusAgain()
    {
        await oAuthDriver.CheckSession();
    }

    [Given("a reader session exists for {string} via {string} with avatar {string}")]
    public async Task GivenAReaderSessionExistsForViaWithAvatar(
        string displayName,
        string providerName,
        string avatarUrl)
    {
        var provider = Enum.Parse<AuthProvider>(providerName, ignoreCase: true);
        var session = ReaderSession.Create(
            displayName,
            avatarUrl,
            provider,
            $"{providerName.ToLowerInvariant()}-{displayName.ToLowerInvariant().Replace(' ', '-')}",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30));

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReaderSessionRepository>();
        await repository.SaveAsync(session, CancellationToken.None);

        sessionContext.SessionCookie = session.Id.ToString();
    }

    [Then("the response contains avatar URL {string}")]
    public void ThenTheResponseContainsAvatarUrl(string expectedUrl)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("avatarUrl").GetString()
            .Should().Be(expectedUrl);
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
