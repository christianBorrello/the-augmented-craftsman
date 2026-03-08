using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;
using TacBlog.Acceptance.Tests.Support;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class LikeSteps
{
    private readonly LikeApiDriver _likeDriver;
    private readonly ApiContext _apiContext;
    private string _visitorId = Guid.NewGuid().ToString();
    private JsonDocument? _actionResponse;

    public LikeSteps(LikeApiDriver likeDriver, ApiContext apiContext)
    {
        _likeDriver = likeDriver;
        _apiContext = apiContext;
    }

    [Given("{string} has {int} likes")]
    public void GivenPostHasLikes(string title, int count)
    {
        // Starting state: no likes in clean database
    }

    [Given("a visitor has liked {string}")]
    public async Task GivenAVisitorHasLiked(string title)
    {
        var slug = SlugHelper.ToSlug(title);
        await _likeDriver.LikePost(slug, _visitorId);
    }

    [Given("a visitor has not previously liked {string}")]
    public void GivenAVisitorHasNotPreviouslyLiked(string title)
    {
        _visitorId = Guid.NewGuid().ToString();
    }

    [When("the visitor unlikes {string}")]
    public async Task WhenTheVisitorUnlikes(string title)
    {
        var slug = SlugHelper.ToSlug(title);
        await _likeDriver.UnlikePost(slug, _visitorId);
        _actionResponse = _apiContext.LastResponseJson;
    }

    [When("the visitor likes {string}")]
    public async Task WhenTheVisitorLikes(string title)
    {
        var slug = SlugHelper.ToSlug(title);
        await _likeDriver.LikePost(slug, _visitorId);
        _actionResponse = _apiContext.LastResponseJson;
    }

    [Then("the like is recorded successfully")]
    public void ThenTheLikeIsRecordedSuccessfully()
    {
        _apiContext.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        _apiContext.LastResponseJson.Should().NotBeNull();
        _apiContext.LastResponseJson!.RootElement
            .GetProperty("liked").GetBoolean().Should().BeTrue();
    }

    [Then("the like count for {string} is {int}")]
    public async Task ThenTheLikeCountForPostIs(string title, int expectedCount)
    {
        var slug = SlugHelper.ToSlug(title);
        await _likeDriver.GetLikeCount(slug);
        _apiContext.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        _apiContext.LastResponseJson.Should().NotBeNull();
        _apiContext.LastResponseJson!.RootElement
            .GetProperty("count").GetInt32().Should().Be(expectedCount);
    }

    [Then("the response indicates the post is not liked")]
    public void ThenTheResponseIndicatesThePostIsNotLiked()
    {
        _actionResponse.Should().NotBeNull();
        _actionResponse!.RootElement
            .GetProperty("liked").GetBoolean().Should().BeFalse();
    }

    [Then("the response indicates the post is liked")]
    public void ThenTheResponseIndicatesThePostIsLiked()
    {
        _actionResponse.Should().NotBeNull();
        _actionResponse!.RootElement
            .GetProperty("liked").GetBoolean().Should().BeTrue();
    }

    [Given("{string} has been liked by {int} visitors")]
    public async Task GivenPostHasBeenLikedByVisitors(string title, int count)
    {
        var slug = SlugHelper.ToSlug(title);
        for (var i = 0; i < count; i++)
        {
            await _likeDriver.LikePost(slug, Guid.NewGuid().ToString());
        }
    }

    [When("a visitor requests the like count for {string}")]
    public async Task WhenAVisitorRequestsTheLikeCountFor(string title)
    {
        var slug = SlugHelper.ToSlug(title);
        await _likeDriver.GetLikeCount(slug);
    }

    [When("the visitor checks their like status for {string}")]
    public async Task WhenTheVisitorChecksTheirLikeStatusFor(string title)
    {
        var slug = SlugHelper.ToSlug(title);
        await _likeDriver.CheckIfLiked(slug, _visitorId);
        _actionResponse = _apiContext.LastResponseJson;
    }

    [Then("the like count is included in the response")]
    public void ThenTheLikeCountIsIncludedInTheResponse()
    {
        _actionResponse.Should().NotBeNull();
        _actionResponse!.RootElement
            .GetProperty("count").GetInt32().Should().BeGreaterOrEqualTo(0);
    }
}
