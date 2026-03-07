using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;
using TacBlog.Acceptance.Tests.Support;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class ImageSteps(
    ImageApiDriver imageDriver,
    ApiContext apiContext,
    ScenarioContext scenarioContext,
    TacBlogWebApplicationFactory factory)
{
    private const string StoredImageUrlKey = "StoredImageUrl";
    private const string StoredSlugKey = "StoredSlug";

    // -- Epic 3: UploadImage (US-030) --

    [When("Christian uploads an image {string}")]
    public async Task WhenChristianUploadsAnImage(string fileName)
    {
        byte[] fakePngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        await imageDriver.UploadImage(fileName, fakePngBytes);
    }

    [Then("the response contains a URL for the uploaded image")]
    public void ThenTheResponseContainsAUrlForTheUploadedImage()
    {
        var url = GetResponseProperty("url");
        url.Should().StartWith("https://");
    }

    [When("Christian uploads a file {string} as an image")]
    public async Task WhenChristianUploadsAFileAsAnImage(string fileName)
    {
        byte[] fakeContent = [0x25, 0x50, 0x44, 0x46];
        await imageDriver.UploadNonImageFile(fileName, fakeContent);
    }

    [Given("the image storage service is temporarily unavailable")]
    public void GivenTheImageStorageServiceIsTemporarilyUnavailable()
    {
        var stub = factory.Services.GetRequiredService<StubImageStorage>();
        stub.ShouldFail = true;
    }

    // -- Epic 3: SetFeaturedImage (US-031) --

    [Given("an image has been uploaded with URL {string}")]
    public void GivenAnImageHasBeenUploadedWithUrl(string url)
    {
        scenarioContext[StoredImageUrlKey] = url;
        CaptureSlugFromLastResponse();
    }

    [When("Christian sets the featured image on the post")]
    public async Task WhenChristianSetsTheFeaturedImageOnThePost()
    {
        var slug = (string)scenarioContext[StoredSlugKey];
        var imageUrl = (string)scenarioContext[StoredImageUrlKey];
        await imageDriver.SetFeaturedImage(slug, imageUrl);
    }

    [Then("the post has the featured image URL")]
    public void ThenThePostHasTheFeaturedImageUrl()
    {
        var imageUrl = GetResponseProperty("featuredImageUrl");
        var storedUrl = (string)scenarioContext[StoredImageUrlKey];
        imageUrl.Should().Be(storedUrl);
    }

    [When("Christian sets the featured image URL to {string}")]
    public async Task WhenChristianSetsTheFeaturedImageUrlTo(string url)
    {
        CaptureSlugFromLastResponse();
        var slug = (string)scenarioContext[StoredSlugKey];
        await imageDriver.SetFeaturedImage(slug, url);
    }

    // -- Epic 3: RemoveFeaturedImage (US-032) --

    [When("Christian removes the featured image from the post")]
    public async Task WhenChristianRemovesTheFeaturedImageFromThePost()
    {
        CaptureSlugFromLastResponse();
        var slug = (string)scenarioContext[StoredSlugKey];
        await imageDriver.RemoveFeaturedImage(slug);
    }

    [When("Christian removes the featured image from a non-existent post")]
    public async Task WhenChristianRemovesTheFeaturedImageFromANonExistentPost()
    {
        await imageDriver.RemoveFeaturedImage("non-existent-post-slug");
    }

    // -- Helpers --

    private string? GetResponseProperty(string propertyName)
    {
        apiContext.LastResponseJson.Should().NotBeNull();
        return apiContext.LastResponseJson!.RootElement
            .GetProperty(propertyName).GetString();
    }

    private void CaptureSlugFromLastResponse()
    {
        if (scenarioContext.ContainsKey(StoredSlugKey))
            return;

        scenarioContext[StoredSlugKey] = GetResponseProperty("slug")!;
    }
}
