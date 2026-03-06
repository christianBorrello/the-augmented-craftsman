using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class TagSteps
{
    private readonly TagApiDriver _tagDriver;
    private readonly ApiContext _apiContext;

    public TagSteps(TagApiDriver tagDriver, ApiContext apiContext)
    {
        _tagDriver = tagDriver;
        _apiContext = apiContext;
    }

    // ── Epic 2: CreateTag (US-020) ──

    [When("Christian creates a tag with name {string}")]
    public async Task WhenChristianCreatesATagWithName(string name)
    {
        throw new PendingStepException();
    }

    [Then("the tag {string} is created with slug {string}")]
    public void ThenTheTagIsCreatedWithSlug(string name, string slug)
    {
        throw new PendingStepException();
    }

    [Given("a tag {string} already exists")]
    public async Task GivenATagAlreadyExists(string name)
    {
        throw new PendingStepException();
    }

    [When("Christian creates a tag with an empty name")]
    public async Task WhenChristianCreatesATagWithAnEmptyName()
    {
        throw new PendingStepException();
    }

    [When("Christian creates a tag with a name of {int} characters")]
    public async Task WhenChristianCreatesATagWithANameOfNCharacters(int length)
    {
        throw new PendingStepException();
    }

    // ── Epic 2: ListTags (US-021) ──

    [Given("these tags exist with post counts:")]
    public async Task GivenTheseTagsExistWithPostCounts(DataTable table)
    {
        throw new PendingStepException();
    }

    [Then("all tags are returned alphabetically with their post counts")]
    public void ThenAllTagsAreReturnedAlphabeticallyWithTheirPostCounts()
    {
        throw new PendingStepException();
    }

    [Then("the tag list is empty")]
    public void ThenTheTagListIsEmpty()
    {
        throw new PendingStepException();
    }

    // ── Epic 2: RenameTag (US-022) ──

    [Given("a tag {string} exists linked to {int} posts")]
    public async Task GivenATagExistsLinkedToPosts(string name, int postCount)
    {
        throw new PendingStepException();
    }

    [When("Christian renames the tag {string} to {string}")]
    public async Task WhenChristianRenamesTheTagTo(string oldName, string newName)
    {
        throw new PendingStepException();
    }

    [Then("the tag name is updated to {string}")]
    public void ThenTheTagNameIsUpdatedTo(string name)
    {
        throw new PendingStepException();
    }

    [Then("the tag slug is updated to {string}")]
    public void ThenTheTagSlugIsUpdatedTo(string slug)
    {
        throw new PendingStepException();
    }

    [Then("all {int} linked posts now show {string}")]
    public void ThenAllLinkedPostsNowShow(int count, string tagName)
    {
        throw new PendingStepException();
    }

    [Given("tags {string} and {string} exist")]
    public async Task GivenTagsExist(string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [When("Christian renames {string} to {string}")]
    public async Task WhenChristianRenamesTo(string oldName, string newName)
    {
        throw new PendingStepException();
    }

    // ── Epic 2: DeleteTag (US-023) ──

    [When("Christian deletes the tag {string}")]
    public async Task WhenChristianDeletesTheTag(string name)
    {
        throw new PendingStepException();
    }

    [Then("the tag {string} is removed")]
    public void ThenTheTagIsRemoved(string name)
    {
        throw new PendingStepException();
    }

    [Then("the {int} previously linked posts no longer have the tag {string}")]
    public void ThenTheLinkedPostsNoLongerHaveTheTag(int count, string tagName)
    {
        throw new PendingStepException();
    }

    [Then("the {int} posts still exist")]
    public void ThenThePostsStillExist(int count)
    {
        throw new PendingStepException();
    }

    [When("Christian tries to delete a non-existent tag")]
    public async Task WhenChristianTriesToDeleteANonExistentTag()
    {
        throw new PendingStepException();
    }

    // ── Epic 2: AssociateTagsWithPosts (US-024) ──

    [Given("tags {string}, {string}, and {string} exist")]
    public async Task GivenThreeTagsExist(string tag1, string tag2, string tag3)
    {
        throw new PendingStepException();
    }

    [When("Christian adds tags {string} and {string} to the post")]
    public async Task WhenChristianAddsTagsToThePost(string tag1, string tag2)
    {
        throw new PendingStepException();
    }

    [Given("the tag {string} does not exist")]
    public void GivenTheTagDoesNotExist(string name)
    {
    }

    [When("Christian adds a new tag {string} to the post")]
    public async Task WhenChristianAddsANewTagToThePost(string name)
    {
        throw new PendingStepException();
    }

    [Then("the tag is associated with the post")]
    public void ThenTheTagIsAssociatedWithThePost()
    {
        throw new PendingStepException();
    }

    [When("Christian removes the tag {string} from the post")]
    public async Task WhenChristianRemovesTheTagFromThePost(string name)
    {
        throw new PendingStepException();
    }

    [Then("the post has only the tag {string}")]
    public void ThenThePostHasOnlyTheTag(string tag)
    {
        throw new PendingStepException();
    }

    [Then("the tag {string} still exists")]
    public void ThenTheTagStillExists(string name)
    {
        throw new PendingStepException();
    }

    [Then("the tags {string} and {string} still exist")]
    public void ThenTheTagsStillExist(string tag1, string tag2)
    {
        throw new PendingStepException();
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
}
