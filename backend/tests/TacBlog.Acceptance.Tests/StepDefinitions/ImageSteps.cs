using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class ImageSteps
{
    private readonly ImageApiDriver _imageDriver;
    private readonly ApiContext _apiContext;

    public ImageSteps(ImageApiDriver imageDriver, ApiContext apiContext)
    {
        _imageDriver = imageDriver;
        _apiContext = apiContext;
    }

    // ── Epic 3: UploadImage (US-030) ──

    [When("Christian uploads an image {string}")]
    public async Task WhenChristianUploadsAnImage(string fileName)
    {
        throw new PendingStepException();
    }

    [Then("the response contains a URL for the uploaded image")]
    public void ThenTheResponseContainsAUrlForTheUploadedImage()
    {
        throw new PendingStepException();
    }

    [When("Christian uploads a file {string} as an image")]
    public async Task WhenChristianUploadsAFileAsAnImage(string fileName)
    {
        throw new PendingStepException();
    }

    [Given("the image storage service is temporarily unavailable")]
    public void GivenTheImageStorageServiceIsTemporarilyUnavailable()
    {
        throw new PendingStepException();
    }

    // ── Epic 3: SetFeaturedImage (US-031) ──

    [Given("an image has been uploaded with URL {string}")]
    public async Task GivenAnImageHasBeenUploadedWithUrl(string url)
    {
        throw new PendingStepException();
    }

    [When("Christian sets the featured image on the post")]
    public async Task WhenChristianSetsTheFeaturedImageOnThePost()
    {
        throw new PendingStepException();
    }

    [Then("the post has the featured image URL")]
    public void ThenThePostHasTheFeaturedImageUrl()
    {
        throw new PendingStepException();
    }

    [When("Christian sets the featured image URL to {string}")]
    public async Task WhenChristianSetsTheFeaturedImageUrlTo(string url)
    {
        throw new PendingStepException();
    }

    // ── Epic 3: RemoveFeaturedImage (US-032) ──

    [When("Christian removes the featured image from the post")]
    public async Task WhenChristianRemovesTheFeaturedImageFromThePost()
    {
        throw new PendingStepException();
    }

    [When("Christian removes the featured image from a non-existent post")]
    public async Task WhenChristianRemovesTheFeaturedImageFromANonExistentPost()
    {
        throw new PendingStepException();
    }
}
