using FluentAssertions;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class ModerationSteps(
    CommentApiDriver commentDriver,
    ApiContext apiContext,
    AuthContext authContext,
    ReaderSessionContext sessionContext)
{
    private readonly Dictionary<string, Guid> _commentIdsByDisplayName = new();

    [When("the admin deletes the comment by {string} on {string}")]
    public async Task WhenTheAdminDeletesTheCommentByOn(string displayName, string postSlug)
    {
        var commentId = await FindCommentIdByDisplayName(displayName, postSlug);
        await commentDriver.DeleteComment(postSlug, commentId, authContext.ApiKey);
    }

    [When("the admin deletes a non-existent comment on {string}")]
    public async Task WhenTheAdminDeletesANonExistentCommentOn(string postSlug)
    {
        await commentDriver.DeleteComment(postSlug, Guid.NewGuid(), authContext.ApiKey);
    }

    [When("an unauthenticated user deletes the comment by {string} on {string}")]
    public async Task WhenAnUnauthenticatedUserDeletesTheCommentByOn(string displayName, string postSlug)
    {
        var commentId = await FindCommentIdByDisplayName(displayName, postSlug);
        await commentDriver.DeleteComment(postSlug, commentId);
    }

    [When("the reader deletes the comment by {string} on {string}")]
    public async Task WhenTheReaderDeletesTheCommentByOn(string displayName, string postSlug)
    {
        var commentId = await FindCommentIdByDisplayName(displayName, postSlug);
        await commentDriver.DeleteCommentWithReaderSession(postSlug, commentId);
    }

    [When("the admin lists all comments")]
    public async Task WhenTheAdminListsAllComments()
    {
        await commentDriver.GetAdminComments(authContext.ApiKey);
    }

    [When("an unauthenticated user lists all comments")]
    public async Task WhenAnUnauthenticatedUserListsAllComments()
    {
        await commentDriver.GetAdminComments();
    }

    [Then("the admin comments list contains {int} comments")]
    public void ThenTheAdminCommentsListContainsComments(int expectedCount)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        var array = apiContext.LastResponseJson!.RootElement;
        array.GetArrayLength().Should().Be(expectedCount);
    }

    private async Task<Guid> FindCommentIdByDisplayName(string displayName, string postSlug)
    {
        // Fetch comments for the post to find the ID by display name
        await commentDriver.GetComments(postSlug);
        var comments = apiContext.LastResponseJson!.RootElement.GetProperty("comments");
        foreach (var comment in comments.EnumerateArray())
        {
            if (comment.GetProperty("displayName").GetString() == displayName)
                return comment.GetProperty("id").GetGuid();
        }

        throw new InvalidOperationException($"No comment found by '{displayName}' on '{postSlug}'");
    }
}
