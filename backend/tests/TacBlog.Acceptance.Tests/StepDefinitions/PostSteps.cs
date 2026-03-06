using FluentAssertions;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class PostSteps
{
    private readonly PostApiDriver _postDriver;
    private readonly TagApiDriver _tagDriver;
    private readonly ApiContext _apiContext;

    public PostSteps(PostApiDriver postDriver, TagApiDriver tagDriver, ApiContext apiContext)
    {
        _postDriver = postDriver;
        _tagDriver = tagDriver;
        _apiContext = apiContext;
    }

    // ── Epic 0: Walking Skeleton (US-001, US-002) ──

    [Given("the blog system is running")]
    public void GivenTheBlogSystemIsRunning()
    {
    }

    [Given("a post exists with slug {string} and title {string}")]
    public async Task GivenAPostExistsWithSlugAndTitle(string slug, string title)
    {
        await _postDriver.CreatePost(title, $"Content for {title}");
    }

    [Given("no post exists with slug {string}")]
    public void GivenNoPostExistsWithSlug(string slug)
    {
    }

    [When("a POST request is sent to {string} with:")]
    public async Task WhenAPostRequestIsSentToWith(string path, DataTable table)
    {
        var title = table.Rows.First(r => r["title"] != null)["title"];
        var content = table.Rows.First(r => r["content"] != null)["content"];
        await _postDriver.CreatePost(title, content);
    }

    [When("a GET request is sent to {string}")]
    public async Task WhenAGetRequestIsSentTo(string path)
    {
        if (path.StartsWith("/api/posts/"))
        {
            var slug = path.Replace("/api/posts/", "");
            await _postDriver.GetPostBySlug(slug);
        }
        else if (path == "/api/posts")
        {
            await _postDriver.ListPosts();
        }
        else if (path == "/api/tags")
        {
            await _tagDriver.ListTags();
        }
    }

    [Then("the response contains a post with slug {string}")]
    public void ThenTheResponseContainsAPostWithSlug(string expectedSlug)
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        _apiContext.LastResponseJson!.RootElement
            .GetProperty("slug").GetString().Should().Be(expectedSlug);
    }

    [Then("the post can be retrieved")]
    public void ThenThePostCanBeRetrieved()
    {
    }

    [Then("the response contains title {string}")]
    public void ThenTheResponseContainsTitle(string expectedTitle)
    {
        _apiContext.LastResponseJson!.RootElement
            .GetProperty("title").GetString().Should().Be(expectedTitle);
    }

    [Then("the response contains content {string}")]
    public void ThenTheResponseContainsContent(string expectedContent)
    {
        _apiContext.LastResponseJson!.RootElement
            .GetProperty("content").GetString().Should().Be(expectedContent);
    }

    // ── Epic 1: CreatePost (US-011, US-012) ──

    [When("Christian creates a post with:")]
    public async Task WhenChristianCreatesAPostWith(DataTable table)
    {
        throw new PendingStepException();
    }

    [When("assigns tags {string} and {string} to the post")]
    public async Task WhenAssignsTagsToThePost(string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [Then("a draft post is created with slug {string}")]
    public void ThenADraftPostIsCreatedWithSlug(string slug)
    {
        throw new PendingStepException();
    }

    [Then("the post status is {string}")]
    public void ThenThePostStatusIs(string status)
    {
        throw new PendingStepException();
    }

    [Then("the post has tags {string} and {string}")]
    public void ThenThePostHasTags(string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [Then("the post has no tags")]
    public void ThenThePostHasNoTags()
    {
        throw new PendingStepException();
    }

    [Then("the post has no featured image")]
    public void ThenThePostHasNoFeaturedImage()
    {
        throw new PendingStepException();
    }

    [Given("a post exists with slug {string}")]
    public async Task GivenAPostExistsWithSlug(string slug)
    {
        throw new PendingStepException();
    }

    [When("Christian creates a post with content including a code example")]
    public async Task WhenChristianCreatesAPostWithContentIncludingACodeExample()
    {
        throw new PendingStepException();
    }

    [Then("the post content is stored with the raw content preserved")]
    public void ThenThePostContentIsStoredWithTheRawContentPreserved()
    {
        throw new PendingStepException();
    }

    [When("Christian creates a post with title {string}")]
    public async Task WhenChristianCreatesAPostWithTitle(string title)
    {
        throw new PendingStepException();
    }

    [When("Christian creates a post with title {string} and no image")]
    public async Task WhenChristianCreatesAPostWithTitleAndNoImage(string title)
    {
        throw new PendingStepException();
    }

    [Then("the generated slug is {string}")]
    public void ThenTheGeneratedSlugIs(string expectedSlug)
    {
        throw new PendingStepException();
    }

    // ── Epic 1: PreviewPost (US-013) ──

    [Given("a draft post {string} exists with content containing a code example")]
    public async Task GivenADraftPostExistsWithContentContainingCodeExample(string title)
    {
        throw new PendingStepException();
    }

    [When("Christian requests a preview of the post")]
    public async Task WhenChristianRequestsAPreviewOfThePost()
    {
        throw new PendingStepException();
    }

    [Then("the preview shows formatted content with highlighted code examples")]
    public void ThenThePreviewShowsFormattedContentWithHighlightedCodeExamples()
    {
        throw new PendingStepException();
    }

    [Given("a draft post {string} exists with a featured image")]
    public async Task GivenADraftPostExistsWithAFeaturedImage(string title)
    {
        throw new PendingStepException();
    }

    [Then("the preview displays the featured image")]
    public void ThenThePreviewDisplaysTheFeaturedImage()
    {
        throw new PendingStepException();
    }

    [Given("a draft post {string} exists with tags {string} and {string}")]
    public async Task GivenADraftPostExistsWithTags(string title, string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [Given("a draft post {string} exists with tags {string}")]
    public async Task GivenADraftPostExistsWithOneTag(string title, string tag)
    {
        throw new PendingStepException();
    }

    [Then("the preview displays tag badges for {string} and {string}")]
    public void ThenThePreviewDisplaysTagBadgesFor(string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [When("Christian requests a preview of a non-existent post")]
    public async Task WhenChristianRequestsAPreviewOfANonExistentPost()
    {
        throw new PendingStepException();
    }

    // ── Epic 1: PublishPost (US-014) ──

    [Given("a draft post {string} exists")]
    public async Task GivenADraftPostExists(string title)
    {
        throw new PendingStepException();
    }

    [Given("a draft post {string} exists without a featured image")]
    public async Task GivenADraftPostExistsWithoutAFeaturedImage(string title)
    {
        throw new PendingStepException();
    }

    [When("Christian publishes the post")]
    public async Task WhenChristianPublishesThePost()
    {
        throw new PendingStepException();
    }

    [Then("the post status changes to {string}")]
    public void ThenThePostStatusChangesTo(string status)
    {
        throw new PendingStepException();
    }

    [Then("the post has a publish date of today")]
    public void ThenThePostHasAPublishDateOfToday()
    {
        throw new PendingStepException();
    }

    [Given("a draft post {string} exists with tags {string} and content about walking skeletons")]
    public async Task GivenADraftPostExistsWithTagsAndContent(string title, string tag)
    {
        throw new PendingStepException();
    }

    [Then("the post title is {string}")]
    public void ThenThePostTitleIs(string title)
    {
        throw new PendingStepException();
    }

    [Then("the post tags include {string}")]
    public void ThenThePostTagsInclude(string tag)
    {
        throw new PendingStepException();
    }

    [Then("the post content is unchanged")]
    public void ThenThePostContentIsUnchanged()
    {
        throw new PendingStepException();
    }

    [Given("a published post {string} exists")]
    public async Task GivenAPublishedPostExists(string title)
    {
        throw new PendingStepException();
    }

    [When("Christian attempts to publish the post again")]
    public async Task WhenChristianAttemptsToPublishThePostAgain()
    {
        throw new PendingStepException();
    }

    // ── Epic 1: EditPost (US-015) ──

    [Given("a published post {string} exists with slug {string}")]
    public async Task GivenAPublishedPostExistsWithSlug(string title, string slug)
    {
        throw new PendingStepException();
    }

    [When("Christian updates the post with:")]
    public async Task WhenChristianUpdatesThePostWith(DataTable table)
    {
        throw new PendingStepException();
    }

    [Then("the slug remains {string}")]
    public void ThenTheSlugRemains(string slug)
    {
        throw new PendingStepException();
    }

    [Given("a draft post exists with title {string} and slug {string}")]
    public async Task GivenADraftPostExistsWithTitleAndSlug(string title, string slug)
    {
        throw new PendingStepException();
    }

    [When("Christian updates the post title to {string}")]
    public async Task WhenChristianUpdatesThePostTitleTo(string title)
    {
        throw new PendingStepException();
    }

    [Given("a post exists with:")]
    public async Task GivenAPostExistsWith(DataTable table)
    {
        throw new PendingStepException();
    }

    [When("Christian retrieves the post for editing")]
    public async Task WhenChristianRetrievesThePostForEditing()
    {
        throw new PendingStepException();
    }

    [Then("the response contains:")]
    public void ThenTheResponseContainsTable(DataTable table)
    {
        throw new PendingStepException();
    }

    [When("Christian tries to update a non-existent post")]
    public async Task WhenChristianTriesToUpdateANonExistentPost()
    {
        throw new PendingStepException();
    }

    [When("Christian updates the post with an empty title")]
    public async Task WhenChristianUpdatesThePostWithAnEmptyTitle()
    {
        throw new PendingStepException();
    }

    // ── Epic 1: DeletePost (US-016) ──

    [Given("a post {string} exists")]
    public async Task GivenAPostExists(string title)
    {
        throw new PendingStepException();
    }

    [Given("a post {string} exists with tags {string} and {string}")]
    public async Task GivenAPostExistsWithTags(string title, string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [When("Christian deletes the post {string}")]
    public async Task WhenChristianDeletesThePost(string title)
    {
        throw new PendingStepException();
    }

    [Then("{string} no longer appears in the post list")]
    public void ThenNoLongerAppearsInThePostList(string title)
    {
        throw new PendingStepException();
    }

    [When("Christian tries to delete a non-existent post")]
    public async Task WhenChristianTriesToDeleteANonExistentPost()
    {
        throw new PendingStepException();
    }

    // ── Epic 1: ListPosts (US-017) ──

    [Given("these posts exist:")]
    public async Task GivenThesePostsExist(DataTable table)
    {
        throw new PendingStepException();
    }

    [When("Christian requests the admin post list")]
    public async Task WhenChristianRequestsTheAdminPostList()
    {
        throw new PendingStepException();
    }

    [Then("posts are returned in reverse chronological order")]
    public void ThenPostsAreReturnedInReverseChronologicalOrder()
    {
        throw new PendingStepException();
    }

    [Then("each post contains title, status, date, and id")]
    public void ThenEachPostContainsTitleStatusDateAndId()
    {
        throw new PendingStepException();
    }

    [Given("{int} draft posts and {int} published posts exist")]
    public async Task GivenDraftPostsAndPublishedPostsExist(int drafts, int published)
    {
        throw new PendingStepException();
    }

    [Then("{int} posts are returned")]
    public void ThenPostsAreReturned(int count)
    {
        throw new PendingStepException();
    }

    [Then("the post list is empty")]
    public void ThenThePostListIsEmpty()
    {
        throw new PendingStepException();
    }

    // ── Epic 4: Homepage (US-040) ──

    [Given("these published posts exist:")]
    public async Task GivenThesePublishedPostsExist(DataTable table)
    {
        throw new PendingStepException();
    }

    [Then("the posts are returned in reverse chronological order")]
    public void ThenThePostsAreReturnedInReverseChronologicalOrder()
    {
        throw new PendingStepException();
    }

    [Then("each post contains title, slug, date, tags, and excerpt")]
    public void ThenEachPostContainsTitleSlugDateTagsAndExcerpt()
    {
        throw new PendingStepException();
    }

    [Given("no published posts exist")]
    public void GivenNoPublishedPostsExist()
    {
    }

    // ── Epic 4: BrowseAllPosts (US-041) ──

    [Given("{int} published posts and {int} draft posts exist")]
    public async Task GivenPublishedPostsAndDraftPostsExist(int published, int drafts)
    {
        throw new PendingStepException();
    }

    [When("a reader requests all published posts")]
    public async Task WhenAReaderRequestsAllPublishedPosts()
    {
        throw new PendingStepException();
    }

    [Then("only the {int} published posts are returned")]
    public void ThenOnlyThePublishedPostsAreReturned(int count)
    {
        throw new PendingStepException();
    }

    [Then("draft posts are not included")]
    public void ThenDraftPostsAreNotIncluded()
    {
        throw new PendingStepException();
    }

    [Given("a published post {string} exists with tags {string} and {string}")]
    public async Task GivenAPublishedPostExistsWithTags(string title, string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [Then("each post contains title, slug, publishedAt, tags, and excerpt")]
    public void ThenEachPostContainsTitleSlugPublishedAtTagsAndExcerpt()
    {
        throw new PendingStepException();
    }

    [Then("post content is not included in the list response")]
    public void ThenPostContentIsNotIncludedInTheListResponse()
    {
        throw new PendingStepException();
    }

    // ── Epic 4: FilterPostsByTag (US-042) ──

    [When("a reader filters posts by tag {string}")]
    public async Task WhenAReaderFiltersPostsByTag(string tag)
    {
        throw new PendingStepException();
    }

    [Then("only posts tagged {string} are returned:")]
    public void ThenOnlyPostsTaggedAreReturned(string tag, DataTable table)
    {
        throw new PendingStepException();
    }

    [Then("{string} is not included")]
    public void ThenIsNotIncluded(string title)
    {
        throw new PendingStepException();
    }

    [Given("no posts are tagged {string}")]
    public void GivenNoPostsAreTagged(string tag)
    {
    }

    // ── Epic 4: ReadSinglePost (US-043) ──

    [Given("a published post exists:")]
    public async Task GivenAPublishedPostExistsTable(DataTable table)
    {
        throw new PendingStepException();
    }

    [When("a reader requests the post with slug {string}")]
    public async Task WhenAReaderRequestsThePostWithSlug(string slug)
    {
        throw new PendingStepException();
    }

    [Then("the response contains the full post with title, content, tags, image, and publishedAt")]
    public void ThenTheResponseContainsTheFullPost()
    {
        throw new PendingStepException();
    }

    // ── Epic 4: RelatedPosts (US-044) ──

    [When("a reader requests related posts for {string}")]
    public async Task WhenAReaderRequestsRelatedPostsFor(string slug)
    {
        throw new PendingStepException();
    }

    [Then("the related posts include:")]
    public void ThenTheRelatedPostsInclude(DataTable table)
    {
        throw new PendingStepException();
    }

    [Then("{string} is not in the related posts")]
    public void ThenIsNotInTheRelatedPosts(string title)
    {
        throw new PendingStepException();
    }

    [Then("the first related post is {string}")]
    public void ThenTheFirstRelatedPostIs(string title)
    {
        throw new PendingStepException();
    }

    [Then("{string} appears before {string}")]
    public void ThenAppearsBefore(string title1, string title2)
    {
        throw new PendingStepException();
    }

    [Then("exactly {int} related posts are returned")]
    public void ThenExactlyRelatedPostsAreReturned(int count)
    {
        throw new PendingStepException();
    }

    [Given("only one post exists tagged {string}")]
    public async Task GivenOnlyOnePostExistsTagged(string tag)
    {
        throw new PendingStepException();
    }

    [When("a reader requests related posts for that post")]
    public async Task WhenAReaderRequestsRelatedPostsForThatPost()
    {
        throw new PendingStepException();
    }

    [Then("the related posts list is empty")]
    public void ThenTheRelatedPostsListIsEmpty()
    {
        throw new PendingStepException();
    }

    [Then("the post is still accessible")]
    public void ThenThePostIsStillAccessible()
    {
        throw new PendingStepException();
    }
}
