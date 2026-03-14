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
public sealed class CommentSteps(
    CommentApiDriver commentDriver,
    ApiContext apiContext,
    TacBlogWebApplicationFactory factory)
{
    private readonly Dictionary<string, Guid> _commentIdsByDisplayName = new();

    [Given("{string} has a comment by {string} via {string} saying {string}")]
    public async Task GivenPostHasACommentByViaSaying(string postSlug, string displayName, string providerName, string text)
    {
        var provider = Enum.Parse<AuthProvider>(providerName, ignoreCase: true);
        var comment = Comment.Create(
            new Slug(postSlug),
            displayName,
            null,
            provider,
            new CommentText(text),
            DateTime.UtcNow);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        await repository.SaveAsync(comment, CancellationToken.None);

        _commentIdsByDisplayName[displayName] = comment.Id.Value;
    }

    [When("the reader posts a comment {string} on {string}")]
    public async Task WhenTheReaderPostsACommentOn(string text, string postSlug)
    {
        await commentDriver.PostComment(postSlug, text);
    }

    [When("the reader posts a comment of {int} characters on {string}")]
    public async Task WhenTheReaderPostsACommentOfCharactersOn(int charCount, string postSlug)
    {
        var text = new string('A', charCount);
        await commentDriver.PostComment(postSlug, text);
    }

    [When("an unauthenticated reader posts a comment {string} on {string}")]
    public async Task WhenAnUnauthenticatedReaderPostsACommentOn(string text, string postSlug)
    {
        await commentDriver.PostCommentUnauthenticated(postSlug, text);
    }

    [When("a reader requests comments for {string}")]
    public async Task WhenAReaderRequestsCommentsFor(string postSlug)
    {
        await commentDriver.GetComments(postSlug);
    }

    [When("a reader requests the comment count for {string}")]
    public async Task WhenAReaderRequestsTheCommentCountFor(string postSlug)
    {
        await commentDriver.GetCommentCount(postSlug);
    }

    [Then("the response contains comment text {string}")]
    public void ThenTheResponseContainsCommentText(string expectedText)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("text").GetString()
            .Should().Be(expectedText);
    }

    [Then("the comment text does not contain {string}")]
    public void ThenTheCommentTextDoesNotContain(string forbidden)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("text").GetString()
            .Should().NotContain(forbidden);
    }

    [Then("the comments count is {int}")]
    public void ThenTheCommentsCountIs(int expectedCount)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("count").GetInt32()
            .Should().Be(expectedCount);
    }

    [Then("the comment count is {int}")]
    public void ThenTheCommentCountIs(int expectedCount)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("count").GetInt32()
            .Should().Be(expectedCount);
    }

    [Then("the comments are in chronological order")]
    public void ThenTheCommentsAreInChronologicalOrder()
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        var comments = apiContext.LastResponseJson!.RootElement.GetProperty("comments");
        var dates = comments.EnumerateArray()
            .Select(c => c.GetProperty("createdAt").GetDateTime())
            .ToList();

        dates.Should().BeInAscendingOrder();
    }

    [Then("the comments count for {string} is {int}")]
    public async Task ThenTheCommentsCountForIs(string postSlug, int expectedCount)
    {
        await commentDriver.GetComments(postSlug);
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement.GetProperty("count").GetInt32()
            .Should().Be(expectedCount);
    }
}
