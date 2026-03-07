using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class TagSteps(
    TagApiDriver tagDriver,
    PostApiDriver postDriver,
    AuthApiDriver authDriver,
    ApiContext apiContext,
    ScenarioContext scenarioContext)
{
    private const string CreatedPostIdsKey = "TagSteps_CreatedPostIds";
    private const string TagSlugMapKey = "TagSteps_TagSlugMap";

    // ── Epic 2: CreateTag (US-020) ──

    [When("Christian creates a tag with name {string}")]
    public async Task WhenChristianCreatesATagWithName(string name)
    {
        await tagDriver.CreateTag(name);
    }

    [Then("the tag {string} is created with slug {string}")]
    public async Task ThenTheTagIsCreatedWithSlug(string name, string slug)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        var root = apiContext.LastResponseJson!.RootElement;

        if (root.TryGetProperty("name", out _))
        {
            root.GetProperty("name").GetString().Should().Be(name);
            root.GetProperty("slug").GetString().Should().Be(slug);
            return;
        }

        await tagDriver.ListTags();
        apiContext.LastResponseJson.Should().NotBeNull();
        var tag = apiContext.LastResponseJson!.RootElement
            .EnumerateArray()
            .FirstOrDefault(t => t.GetProperty("name").GetString() == name);

        tag.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            $"tag '{name}' should exist in the tag list");
        tag.GetProperty("slug").GetString().Should().Be(slug);
    }

    [Given("a tag {string} already exists")]
    public async Task GivenATagAlreadyExists(string name)
    {
        await CreateAndStoreTag(name);
    }

    [When("Christian creates a tag with an empty name")]
    public async Task WhenChristianCreatesATagWithAnEmptyName()
    {
        await tagDriver.CreateTag("");
    }

    [When("Christian creates a tag with a name of {int} characters")]
    public async Task WhenChristianCreatesATagWithANameOfNCharacters(int length)
    {
        var name = new string('A', length);
        await tagDriver.CreateTag(name);
    }

    // ── Epic 2: ListTags (US-021) ──

    [Given("these tags exist with post counts:")]
    public async Task GivenTheseTagsExistWithPostCounts(DataTable table)
    {
        await authDriver.Authenticate();

        foreach (var row in table.Rows)
        {
            var tagName = row["name"];
            var postCount = int.Parse(row["post_count"]);

            await CreateAndStoreTag(tagName);
            await CreatePostsWithTag(tagName, postCount);
        }
    }

    [Then("all tags are returned alphabetically with their post counts")]
    public void ThenAllTagsAreReturnedAlphabeticallyWithTheirPostCounts()
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        var tags = apiContext.LastResponseJson!.RootElement.EnumerateArray().ToList();

        tags.Count.Should().Be(4);

        var names = tags.Select(t => t.GetProperty("name").GetString()).ToList();
        names.Should().BeInAscendingOrder();

        var expectedCounts = new Dictionary<string, int>
        {
            ["Architecture"] = 2,
            ["Clean Code"] = 3,
            ["DDD"] = 1,
            ["TDD"] = 5,
        };

        foreach (var tag in tags)
        {
            var name = tag.GetProperty("name").GetString()!;
            var postCount = tag.GetProperty("postCount").GetInt32();
            postCount.Should().Be(expectedCounts[name], $"tag '{name}' should have correct post count");
        }
    }

    [Then("the tag list is empty")]
    public void ThenTheTagListIsEmpty()
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        var tags = apiContext.LastResponseJson!.RootElement.EnumerateArray().ToList();
        tags.Should().BeEmpty();
    }

    // ── Epic 2: RenameTag (US-022) ──

    [Given("a tag {string} exists linked to {int} posts")]
    public async Task GivenATagExistsLinkedToPosts(string name, int postCount)
    {
        await CreateAndStoreTag(name);
        await CreatePostsWithTag(name, postCount);
    }

    [When("Christian renames the tag {string} to {string}")]
    public async Task WhenChristianRenamesTheTagTo(string oldName, string newName)
    {
        var slug = ToSlug(oldName);
        await tagDriver.RenameTag(slug, newName);
    }

    [Then("the tag name is updated to {string}")]
    public void ThenTheTagNameIsUpdatedTo(string name)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement
            .GetProperty("name").GetString().Should().Be(name);
    }

    [Then("the tag slug is updated to {string}")]
    public void ThenTheTagSlugIsUpdatedTo(string slug)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        apiContext.LastResponseJson!.RootElement
            .GetProperty("slug").GetString().Should().Be(slug);
    }

    [Then("all {int} linked posts now show {string}")]
    public async Task ThenAllLinkedPostsNowShow(int count, string tagName)
    {
        var linkedPosts = await FetchTrackedPosts();
        linkedPosts.Should().HaveCount(count);

        foreach (var post in linkedPosts)
        {
            ParsePostTags(post).Should().Contain(tagName);
        }
    }

    [Given("tags {string} and {string} exist")]
    public async Task GivenTagsExist(string tag1, string tag2)
    {
        await CreateAndStoreTag(tag1);
        await CreateAndStoreTag(tag2);
    }

    [When("Christian renames {string} to {string}")]
    public async Task WhenChristianRenamesTo(string oldName, string newName)
    {
        var slug = ToSlug(oldName);
        await tagDriver.RenameTag(slug, newName);
    }

    // ── Epic 2: DeleteTag (US-023) ──

    [When("Christian deletes the tag {string}")]
    public async Task WhenChristianDeletesTheTag(string name)
    {
        var slug = ToSlug(name);
        await tagDriver.DeleteTag(slug);
    }

    [Then("the tag {string} is removed")]
    public async Task ThenTheTagIsRemoved(string name)
    {
        var tagNames = await FetchTagNames();
        tagNames.Should().NotContain(name);
    }

    [Then("the {int} previously linked posts no longer have the tag {string}")]
    public async Task ThenTheLinkedPostsNoLongerHaveTheTag(int count, string tagName)
    {
        var linkedPosts = await FetchTrackedPosts();
        linkedPosts.Should().HaveCount(count);

        foreach (var post in linkedPosts)
        {
            ParsePostTags(post).Should().NotContain(tagName);
        }
    }

    [Then("the {int} posts still exist")]
    public async Task ThenThePostsStillExist(int count)
    {
        var trackedPosts = await FetchTrackedPosts();
        trackedPosts.Should().HaveCount(count);
    }

    [When("Christian tries to delete a non-existent tag")]
    public async Task WhenChristianTriesToDeleteANonExistentTag()
    {
        await tagDriver.DeleteTag("non-existent-tag-slug");
    }

    // ── Epic 2: AssociateTagsWithPosts (US-024) ──

    [Given("tags {string}, {string}, and {string} exist")]
    public async Task GivenThreeTagsExist(string tag1, string tag2, string tag3)
    {
        await CreateAndStoreTag(tag1);
        await CreateAndStoreTag(tag2);
        await CreateAndStoreTag(tag3);
    }

    [When("Christian adds tags {string} and {string} to the post")]
    public async Task WhenChristianAddsTagsToThePost(string tag1, string tag2)
    {
        var postId = GetLastCreatedPostId();
        var (title, content) = await GetPostTitleAndContent(postId);
        await postDriver.UpdatePost(postId, new { title, content, tags = new[] { tag1, tag2 } });
    }

    [Given("the tag {string} does not exist")]
    public void GivenTheTagDoesNotExist(string name)
    {
    }

    [When("Christian adds a new tag {string} to the post")]
    public async Task WhenChristianAddsANewTagToThePost(string name)
    {
        var postId = GetLastCreatedPostId();
        var (title, content) = await GetPostTitleAndContent(postId);
        await postDriver.UpdatePost(postId, new { title, content, tags = new[] { name } });
        scenarioContext["AssociatedTagName"] = name;
    }

    [Then("the tag is associated with the post")]
    public async Task ThenTheTagIsAssociatedWithThePost()
    {
        var postId = GetLastCreatedPostId();
        await postDriver.PreviewPost(postId);

        apiContext.LastResponseJson.Should().NotBeNull();
        var tagName = (string)scenarioContext["AssociatedTagName"];
        var tags = apiContext.LastResponseJson!.RootElement
            .GetProperty("tags").EnumerateArray()
            .Select(t => t.GetString())
            .ToList();
        tags.Should().Contain(tagName);
    }

    [When("Christian removes the tag {string} from the post")]
    public async Task WhenChristianRemovesTheTagFromThePost(string name)
    {
        var postId = GetLastCreatedPostId();
        var (title, content) = await GetPostTitleAndContent(postId);

        var currentTags = apiContext.LastResponseJson!.RootElement
            .GetProperty("tags").EnumerateArray()
            .Select(t => t.GetString()!)
            .Where(t => t != name)
            .ToArray();

        await postDriver.UpdatePost(postId, new { title, content, tags = currentTags });
    }

    [Then("the post has only the tag {string}")]
    public void ThenThePostHasOnlyTheTag(string tag)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        var tags = apiContext.LastResponseJson!.RootElement
            .GetProperty("tags").EnumerateArray()
            .Select(t => t.GetString())
            .ToList();
        tags.Should().ContainSingle().Which.Should().Be(tag);
    }

    [Then("the tag {string} still exists")]
    public async Task ThenTheTagStillExists(string name)
    {
        var tagNames = await FetchTagNames();
        tagNames.Should().Contain(name);
    }

    [Then("the tags {string} and {string} still exist")]
    public async Task ThenTheTagsStillExist(string tag1, string tag2)
    {
        var tagNames = await FetchTagNames();
        tagNames.Should().Contain(tag1);
        tagNames.Should().Contain(tag2);
    }

    // ── Epic 4: BrowseTags (US-045, US-046) ──

    [Given("these tags exist with published post counts:")]
    public async Task GivenTheseTagsExistWithPublishedPostCounts(DataTable table)
    {
        throw new PendingStepException();
    }

    [When("a reader requests all tags")]
    public async Task WhenAReaderRequestsAllTags()
    {
        throw new PendingStepException();
    }

    [Then("all tags are returned with their post counts")]
    public void ThenAllTagsAreReturnedWithTheirPostCounts()
    {
        throw new PendingStepException();
    }

    [Then("tags are sorted alphabetically")]
    public void ThenTagsAreSortedAlphabetically()
    {
        throw new PendingStepException();
    }

    [Given("a tag {string} exists with slug {string} and {int} published posts")]
    public async Task GivenATagExistsWithSlugAndPublishedPosts(string name, string slug, int count)
    {
        throw new PendingStepException();
    }

    [When("a reader requests posts filtered by tag slug {string}")]
    public async Task WhenAReaderRequestsPostsFilteredByTagSlug(string slug)
    {
        throw new PendingStepException();
    }

    [Given("a tag {string} exists with {int} published posts")]
    public async Task GivenATagExistsWithPublishedPosts(string name, int count)
    {
        throw new PendingStepException();
    }

    [Then("{string} is not included in the public tag list")]
    public void ThenIsNotIncludedInThePublicTagList(string name)
    {
        throw new PendingStepException();
    }

    // ── Helpers ──

    private async Task<List<string?>> FetchTagNames()
    {
        await tagDriver.ListTags();
        apiContext.LastResponseJson.Should().NotBeNull();
        return apiContext.LastResponseJson!.RootElement
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();
    }

    private async Task<List<JsonElement>> FetchTrackedPosts()
    {
        await postDriver.GetAdminPosts();
        apiContext.LastResponseJson.Should().NotBeNull();

        var postIds = GetCreatedPostIds();
        return apiContext.LastResponseJson!.RootElement
            .EnumerateArray()
            .Where(p => postIds.Contains(p.GetProperty("id").GetGuid().ToString()))
            .ToList();
    }

    private static List<string?> ParsePostTags(JsonElement post) =>
        post.GetProperty("tags").EnumerateArray()
            .Select(t => t.GetString())
            .ToList();

    private async Task CreateAndStoreTag(string name)
    {
        await tagDriver.CreateTag(name);
        StoreTagSlug(name);
    }

    private async Task CreatePostsWithTag(string tagName, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await postDriver.CreatePostWithTags(
                $"{tagName} Post {i + 1}",
                $"Content for {tagName} post {i + 1}.",
                [tagName]);
            CaptureCreatedPostId();
        }
    }

    private string GetLastCreatedPostId() =>
        (string)scenarioContext["LastCreatedPostId"];

    private async Task<(string Title, string Content)> GetPostTitleAndContent(string postId)
    {
        await postDriver.PreviewPost(postId);
        var root = apiContext.LastResponseJson!.RootElement;
        return (root.GetProperty("title").GetString()!, root.GetProperty("content").GetString()!);
    }

    private void StoreTagSlug(string tagName)
    {
        var map = GetOrCreateTagSlugMap();
        map[tagName] = ToSlug(tagName);
    }

    private Dictionary<string, string> GetOrCreateTagSlugMap()
    {
        if (!scenarioContext.TryGetValue(TagSlugMapKey, out var existing))
        {
            existing = new Dictionary<string, string>();
            scenarioContext[TagSlugMapKey] = existing;
        }
        return (Dictionary<string, string>)existing;
    }

    private void CaptureCreatedPostId()
    {
        if (apiContext.LastResponseJson is null) return;

        var root = apiContext.LastResponseJson.RootElement;
        if (!root.TryGetProperty("id", out var idElement)) return;

        var ids = GetOrCreatePostIds();
        ids.Add(idElement.GetGuid().ToString());
    }

    private List<string> GetOrCreatePostIds()
    {
        if (!scenarioContext.TryGetValue(CreatedPostIdsKey, out var existing))
        {
            existing = new List<string>();
            scenarioContext[CreatedPostIdsKey] = existing;
        }
        return (List<string>)existing;
    }

    private List<string> GetCreatedPostIds()
    {
        if (scenarioContext.TryGetValue(CreatedPostIdsKey, out var existing))
            return (List<string>)existing;
        return [];
    }

    private static string ToSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-{2,}", "-");
        return slug.Trim('-');
    }
}
