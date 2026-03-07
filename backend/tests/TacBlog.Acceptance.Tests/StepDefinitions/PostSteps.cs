using System.Text.Json;
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
    private readonly ImageApiDriver _imageDriver;
    private readonly ApiContext _apiContext;
    private readonly ScenarioContext _scenarioContext;

    private const string LastCreatedPostIdKey = "LastCreatedPostId";
    private const string PostIdsByTitleKey = "PostIdsByTitle";
    private const string LastCreatedContentKey = "LastCreatedContent";
    private const string PendingTitleKey = "PendingTitle";
    private const string PendingContentKey = "PendingContent";

    public PostSteps(
        PostApiDriver postDriver,
        TagApiDriver tagDriver,
        ImageApiDriver imageDriver,
        ApiContext apiContext,
        ScenarioContext scenarioContext)
    {
        _postDriver = postDriver;
        _tagDriver = tagDriver;
        _imageDriver = imageDriver;
        _apiContext = apiContext;
        _scenarioContext = scenarioContext;
    }

    // ── Epic 0: Walking Skeleton (US-001, US-002) ──

    [Given("the blog system is running")]
    public void GivenTheBlogSystemIsRunning()
    {
    }

    [Given("a post exists with slug {string} and title {string}")]
    public async Task GivenAPostExistsWithSlugAndTitle(string slug, string title)
    {
        await _postDriver.CreatePost(title, "This is the **first** post.");
    }

    [Given("no post exists with slug {string}")]
    public void GivenNoPostExistsWithSlug(string slug)
    {
    }

    [When("a POST request is sent to {string} with:")]
    public async Task WhenAPostRequestIsSentToWith(string path, DataTable table)
    {
        var data = table.Rows.ToDictionary(r => r[0], r => r[1]);
        var headerKey = table.Header.First();
        var headerValue = table.Header.Last();
        data[headerKey] = headerValue;

        await _postDriver.CreatePost(data["title"], data["content"]);
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
        GetResponseProperty("slug").Should().Be(expectedSlug);
    }

    [Then("the post can be retrieved")]
    public void ThenThePostCanBeRetrieved()
    {
    }

    [Then("the response contains title {string}")]
    public void ThenTheResponseContainsTitle(string expectedTitle)
    {
        GetResponseProperty("title").Should().Be(expectedTitle);
    }

    [Then("the response contains content {string}")]
    public void ThenTheResponseContainsContent(string expectedContent)
    {
        GetResponseProperty("content").Should().Be(expectedContent);
    }

    // ── Epic 1: CreatePost (US-011, US-012) ──

    [When("Christian creates a post with:")]
    public async Task WhenChristianCreatesAPostWith(DataTable table)
    {
        var data = ParseVerticalTable(table);
        var title = data["title"];
        var content = data["content"];

        _scenarioContext[PendingTitleKey] = title;
        _scenarioContext[PendingContentKey] = content;
        _scenarioContext[LastCreatedContentKey] = content;

        await _postDriver.CreatePost(title, content);
        CapturePostIdFromResponse();
    }

    [When("assigns tags {string} and {string} to the post")]
    public async Task WhenAssignsTagsToThePost(string tag1, string tag2)
    {
        var postId = GetLastCreatedPostId();
        await _postDriver.DeletePost(postId);

        var title = (string)_scenarioContext[PendingTitleKey];
        var content = (string)_scenarioContext[PendingContentKey];

        await _postDriver.CreatePostWithTags(title, content, [tag1, tag2]);
        CapturePostIdFromResponse();
    }

    [Then("a draft post is created with slug {string}")]
    public void ThenADraftPostIsCreatedWithSlug(string slug)
    {
        GetResponseProperty("slug").Should().Be(slug);
    }

    [Then("the post status is {string}")]
    public void ThenThePostStatusIs(string status)
    {
        GetResponseProperty("status").Should().Be(status);
    }

    [Then("the post has tags {string} and {string}")]
    public void ThenThePostHasTags(string tag1, string tag2)
    {
        var tags = GetTagsFromResponse();
        tags.Should().Contain(tag1);
        tags.Should().Contain(tag2);
    }

    [Then("the post has no tags")]
    public void ThenThePostHasNoTags()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var tags = _apiContext.LastResponseJson!.RootElement
            .GetProperty("tags").EnumerateArray().ToList();

        tags.Should().BeEmpty();
    }

    [Then("the post has no featured image")]
    public void ThenThePostHasNoFeaturedImage()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var root = _apiContext.LastResponseJson!.RootElement;

        if (root.TryGetProperty("featuredImageUrl", out var imageUrl))
        {
            imageUrl.ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    [Given("a post exists with slug {string}")]
    public async Task GivenAPostExistsWithSlug(string slug)
    {
        var title = SlugToTitle(slug);
        await _postDriver.CreatePost(title, "Default content for setup.");
        CapturePostIdFromResponse();
    }

    [When("Christian creates a post with content including a code example")]
    public async Task WhenChristianCreatesAPostWithContentIncludingACodeExample()
    {
        var content = "Here is a code example:\n\n```csharp\npublic class Hello\n{\n    public string Greet() => \"Hello\";\n}\n```";
        _scenarioContext[LastCreatedContentKey] = content;
        await _postDriver.CreatePost("Code Example Post", content);
        CapturePostIdFromResponse();
    }

    [Then("the post content is stored with the raw content preserved")]
    public void ThenThePostContentIsStoredWithTheRawContentPreserved()
    {
        var expectedContent = (string)_scenarioContext[LastCreatedContentKey];
        GetResponseProperty("content").Should().Be(expectedContent);
    }

    [When("Christian creates a post with title {string}")]
    public async Task WhenChristianCreatesAPostWithTitle(string title)
    {
        await _postDriver.CreatePost(title, "Default content for slug test.");
        CapturePostIdFromResponse();
    }

    [When("Christian creates a post with title {string} and no image")]
    public async Task WhenChristianCreatesAPostWithTitleAndNoImage(string title)
    {
        await _postDriver.CreatePost(title, "Default content.");
        CapturePostIdFromResponse();
    }

    [Then("the generated slug is {string}")]
    public void ThenTheGeneratedSlugIs(string expectedSlug)
    {
        GetResponseProperty("slug").Should().Be(expectedSlug);
    }

    // ── Epic 1: PreviewPost (US-013) ──

    [Given("a draft post {string} exists with content containing a code example")]
    public async Task GivenADraftPostExistsWithContentContainingCodeExample(string title)
    {
        var content = "## Code Example\n\n```csharp\npublic void Test() { }\n```";
        _scenarioContext[LastCreatedContentKey] = content;
        await _postDriver.CreatePost(title, content);
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [When("Christian requests a preview of the post")]
    public async Task WhenChristianRequestsAPreviewOfThePost()
    {
        var postId = GetLastCreatedPostId();
        await _postDriver.PreviewPost(postId);
    }

    [Then("the preview shows formatted content with highlighted code examples")]
    public void ThenThePreviewShowsFormattedContentWithHighlightedCodeExamples()
    {
        var expectedContent = (string)_scenarioContext[LastCreatedContentKey];
        GetResponseProperty("content").Should().Be(expectedContent);
    }

    [Given("a draft post {string} exists with a featured image")]
    public async Task GivenADraftPostExistsWithAFeaturedImage(string title)
    {
        await _postDriver.CreatePost(title, "Content for featured image test.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);

        var slug = _apiContext.LastResponseJson!.RootElement
            .GetProperty("slug").GetString()!;
        await _imageDriver.SetFeaturedImage(slug, "https://ik.imagekit.io/test/featured.png");
    }

    [Then("the preview displays the featured image")]
    public void ThenThePreviewDisplaysTheFeaturedImage()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var root = _apiContext.LastResponseJson!.RootElement;

        root.TryGetProperty("featuredImageUrl", out _).Should().BeTrue();
    }

    [Given("a draft post {string} exists with tags {string} and {string}")]
    public async Task GivenADraftPostExistsWithTags(string title, string tag1, string tag2)
    {
        await _postDriver.CreatePostWithTags(title, "Content for preview test.", [tag1, tag2]);
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [Given("a draft post {string} exists with tags {string}")]
    public async Task GivenADraftPostExistsWithOneTag(string title, string tag)
    {
        await _postDriver.CreatePostWithTags(title, "Content for tag test.", [tag]);
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [Then("the preview displays tag badges for {string} and {string}")]
    public void ThenThePreviewDisplaysTagBadgesFor(string tag1, string tag2)
    {
        var tags = GetTagsFromResponse();
        tags.Should().Contain(tag1);
        tags.Should().Contain(tag2);
    }

    [When("Christian requests a preview of a non-existent post")]
    public async Task WhenChristianRequestsAPreviewOfANonExistentPost()
    {
        var randomId = Guid.NewGuid().ToString();
        await _postDriver.PreviewPost(randomId);
    }

    // ── Epic 1: PublishPost (US-014) ──

    [Given("a draft post {string} exists")]
    public async Task GivenADraftPostExists(string title)
    {
        await _postDriver.CreatePost(title, "Draft content for publish test.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [Given("a draft post {string} exists without a featured image")]
    public async Task GivenADraftPostExistsWithoutAFeaturedImage(string title)
    {
        await _postDriver.CreatePost(title, "Draft content without image.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [When("Christian publishes the post")]
    public async Task WhenChristianPublishesThePost()
    {
        var postId = GetLastCreatedPostId();
        await _postDriver.PublishPost(postId);
    }

    [Then("the post status changes to {string}")]
    public void ThenThePostStatusChangesTo(string status)
    {
        GetResponseProperty("status").Should().Be(status);
    }

    [Then("the post has a publish date of today")]
    public void ThenThePostHasAPublishDateOfToday()
    {
        var publishedAt = GetResponseProperty("publishedAt");
        publishedAt.Should().NotBeNull();
        var publishedDate = DateTimeOffset.Parse(publishedAt!);
        publishedDate.UtcDateTime.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Given("a draft post {string} exists with tags {string} and content about walking skeletons")]
    public async Task GivenADraftPostExistsWithTagsAndContent(string title, string tag)
    {
        var content = "The Walking Skeleton pattern is an architectural approach that proves the system works end-to-end.";
        _scenarioContext[LastCreatedContentKey] = content;
        await _postDriver.CreatePostWithTags(title, content, [tag]);
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [Then("the post title is {string}")]
    public void ThenThePostTitleIs(string title)
    {
        GetResponseProperty("title").Should().Be(title);
    }

    [Then("the post tags include {string}")]
    public void ThenThePostTagsInclude(string tag)
    {
        var tags = GetTagsFromResponse();
        tags.Should().Contain(tag);
    }

    [Then("the post content is unchanged")]
    public void ThenThePostContentIsUnchanged()
    {
        var expectedContent = (string)_scenarioContext[LastCreatedContentKey];
        GetResponseProperty("content").Should().Be(expectedContent);
    }

    [Given("a published post {string} exists")]
    public async Task GivenAPublishedPostExists(string title)
    {
        await _postDriver.CreatePost(title, "Published content.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);

        var postId = GetLastCreatedPostId();
        await _postDriver.PublishPost(postId);
    }

    [When("Christian attempts to publish the post again")]
    public async Task WhenChristianAttemptsToPublishThePostAgain()
    {
        var postId = GetLastCreatedPostId();
        await _postDriver.PublishPost(postId);
    }

    // ── Epic 1: EditPost (US-015) ──

    [Given("a published post {string} exists with slug {string}")]
    public async Task GivenAPublishedPostExistsWithSlug(string title, string slug)
    {
        await _postDriver.CreatePost(title, "Content for edit test.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);

        var postId = GetLastCreatedPostId();
        await _postDriver.PublishPost(postId);
    }

    [When("Christian updates the post with:")]
    public async Task WhenChristianUpdatesThePostWith(DataTable table)
    {
        var data = ParseVerticalTable(table);
        var postId = GetLastCreatedPostId();

        await _postDriver.UpdatePost(postId, new { title = data["title"], content = data["content"] });
    }

    [Then("the slug remains {string}")]
    public void ThenTheSlugRemains(string slug)
    {
        GetResponseProperty("slug").Should().Be(slug);
    }

    [Given("a draft post exists with title {string} and slug {string}")]
    public async Task GivenADraftPostExistsWithTitleAndSlug(string title, string slug)
    {
        await _postDriver.CreatePost(title, "Content for slug immutability test.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [When("Christian updates the post title to {string}")]
    public async Task WhenChristianUpdatesThePostTitleTo(string title)
    {
        var postId = GetLastCreatedPostId();
        await _postDriver.UpdatePost(postId, new { title, content = "Content for slug immutability test." });
    }

    [Given("a post exists with:")]
    public async Task GivenAPostExistsWith(DataTable table)
    {
        var data = ParseVerticalTable(table);
        var title = data["title"];

        if (data.TryGetValue("tags", out var tagsRaw) && !string.IsNullOrWhiteSpace(tagsRaw))
        {
            var tags = tagsRaw.Split(',', StringSplitOptions.TrimEntries);
            await _postDriver.CreatePostWithTags(title, "Content for edit form test.", tags);
        }
        else
        {
            await _postDriver.CreatePost(title, "Content for edit form test.");
        }

        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [When("Christian retrieves the post for editing")]
    public async Task WhenChristianRetrievesThePostForEditing()
    {
        var slug = _apiContext.LastResponseJson!.RootElement
            .GetProperty("slug").GetString()!;

        await _postDriver.GetPostBySlug(slug);
    }

    [Then("the response contains:")]
    public void ThenTheResponseContainsTable(DataTable table)
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var root = _apiContext.LastResponseJson!.RootElement;

        var data = ParseVerticalTable(table);

        foreach (var (key, expected) in data)
        {
            root.GetProperty(key).GetString().Should().Be(expected);
        }
    }

    [When("Christian tries to update a non-existent post")]
    public async Task WhenChristianTriesToUpdateANonExistentPost()
    {
        var randomId = Guid.NewGuid().ToString();
        await _postDriver.UpdatePost(randomId, new { title = "Ghost Post", content = "Does not exist." });
    }

    [When("Christian updates the post with an empty title")]
    public async Task WhenChristianUpdatesThePostWithAnEmptyTitle()
    {
        var postId = GetLastCreatedPostId();
        await _postDriver.UpdatePost(postId, new { title = "", content = "Some content." });
    }

    // ── Epic 1: DeletePost (US-016) ──

    [Given("a post {string} exists")]
    public async Task GivenAPostExists(string title)
    {
        await _postDriver.CreatePost(title, "Content for delete test.");
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [Given("a post {string} exists with tags {string} and {string}")]
    public async Task GivenAPostExistsWithTags(string title, string tag1, string tag2)
    {
        await _postDriver.CreatePostWithTags(title, "Content for tag preservation test.", [tag1, tag2]);
        CapturePostIdFromResponse();
        StorePostIdByTitle(title);
    }

    [When("Christian deletes the post {string}")]
    public async Task WhenChristianDeletesThePost(string title)
    {
        var postId = GetPostIdByTitle(title);
        await _postDriver.DeletePost(postId);
    }

    [Then("{string} no longer appears in the post list")]
    public async Task ThenNoLongerAppearsInThePostList(string title)
    {
        await _postDriver.GetAdminPosts();

        _apiContext.LastResponseJson.Should().NotBeNull();
        var titles = _apiContext.LastResponseJson!.RootElement
            .EnumerateArray()
            .Select(p => p.GetProperty("title").GetString())
            .ToList();

        titles.Should().NotContain(title);
    }

    [When("Christian tries to delete a non-existent post")]
    public async Task WhenChristianTriesToDeleteANonExistentPost()
    {
        var randomId = Guid.NewGuid().ToString();
        await _postDriver.DeletePost(randomId);
    }

    // ── Epic 1: ListPosts (US-017) ──

    [Given("these posts exist:")]
    public async Task GivenThesePostsExist(DataTable table)
    {
        foreach (var row in table.Rows)
        {
            var title = row["title"];
            var status = row["status"];

            await _postDriver.CreatePost(title, $"Content for {title}.");
            CapturePostIdFromResponse();
            StorePostIdByTitle(title);

            if (status == "Published")
            {
                var postId = GetLastCreatedPostId();
                await _postDriver.PublishPost(postId);
            }
        }
    }

    [When("Christian requests the admin post list")]
    public async Task WhenChristianRequestsTheAdminPostList()
    {
        await _postDriver.GetAdminPosts();
    }

    [Then("posts are returned in reverse chronological order")]
    public void ThenPostsAreReturnedInReverseChronologicalOrder()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var posts = _apiContext.LastResponseJson!.RootElement.EnumerateArray().ToList();

        posts.Count.Should().BeGreaterThan(1);

        var dates = posts
            .Select(p => DateTime.Parse(p.GetProperty("createdAt").GetString()!))
            .ToList();

        dates.Should().BeInDescendingOrder();
    }

    [Then("each post contains title, status, date, and id")]
    public void ThenEachPostContainsTitleStatusDateAndId()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var posts = _apiContext.LastResponseJson!.RootElement.EnumerateArray().ToList();

        foreach (var post in posts)
        {
            post.TryGetProperty("title", out _).Should().BeTrue();
            post.TryGetProperty("status", out _).Should().BeTrue();
            post.TryGetProperty("createdAt", out _).Should().BeTrue();
            post.TryGetProperty("id", out _).Should().BeTrue();
        }
    }

    [Given("{int} draft posts and {int} published posts exist")]
    public async Task GivenDraftPostsAndPublishedPostsExist(int drafts, int published)
    {
        for (var i = 1; i <= drafts; i++)
        {
            await _postDriver.CreatePost($"Draft Post {i}", $"Draft content {i}.");
            CapturePostIdFromResponse();
        }

        for (var i = 1; i <= published; i++)
        {
            await _postDriver.CreatePost($"Published Post {i}", $"Published content {i}.");
            CapturePostIdFromResponse();
            var postId = GetLastCreatedPostId();
            await _postDriver.PublishPost(postId);
        }
    }

    [Then("{int} posts are returned")]
    public void ThenPostsAreReturned(int count)
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var posts = _apiContext.LastResponseJson!.RootElement.EnumerateArray().ToList();
        posts.Count.Should().Be(count);
    }

    [Then("the post list is empty")]
    public void ThenThePostListIsEmpty()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var posts = _apiContext.LastResponseJson!.RootElement.EnumerateArray().ToList();
        posts.Should().BeEmpty();
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

    // ── Helpers ──

    private string? GetResponseProperty(string propertyName)
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        return _apiContext.LastResponseJson!.RootElement
            .GetProperty(propertyName).GetString();
    }

    private List<string?> GetTagsFromResponse()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        return _apiContext.LastResponseJson!.RootElement
            .GetProperty("tags").EnumerateArray()
            .Select(t => t.GetString())
            .ToList();
    }

    private void CapturePostIdFromResponse()
    {
        if (_apiContext.LastResponseJson is null)
            return;

        var root = _apiContext.LastResponseJson.RootElement;
        if (root.TryGetProperty("id", out var idElement))
        {
            _scenarioContext[LastCreatedPostIdKey] = idElement.GetGuid().ToString();
        }
    }

    private string GetLastCreatedPostId() =>
        (string)_scenarioContext[LastCreatedPostIdKey];

    private void StorePostIdByTitle(string title)
    {
        if (!_scenarioContext.TryGetValue(PostIdsByTitleKey, out var existing))
        {
            existing = new Dictionary<string, string>();
            _scenarioContext[PostIdsByTitleKey] = existing;
        }

        var dict = (Dictionary<string, string>)existing;
        dict[title] = GetLastCreatedPostId();
    }

    private string GetPostIdByTitle(string title)
    {
        var dict = (Dictionary<string, string>)_scenarioContext[PostIdsByTitleKey];
        return dict[title];
    }

    private static string SlugToTitle(string slug) =>
        string.Join(' ', slug.Split('-').Select(word =>
            char.ToUpper(word[0]) + word[1..]));

    private static Dictionary<string, string> ParseVerticalTable(DataTable table)
    {
        var data = table.Rows.ToDictionary(r => r[0], r => r[1]);
        var headerKey = table.Header.First();
        var headerValue = table.Header.Last();
        data[headerKey] = headerValue;
        return data;
    }
}
