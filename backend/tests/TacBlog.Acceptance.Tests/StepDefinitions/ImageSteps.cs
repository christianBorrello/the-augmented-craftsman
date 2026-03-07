using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using TacBlog.Acceptance.Tests.Contexts;
using TacBlog.Acceptance.Tests.Drivers;
using TacBlog.Acceptance.Tests.Support;

namespace TacBlog.Acceptance.Tests.StepDefinitions;

[Binding]
public sealed class ImageSteps
{
    private readonly ImageApiDriver _imageDriver;
    private readonly ApiContext _apiContext;
    private readonly ScenarioContext _scenarioContext;
    private readonly TacBlogWebApplicationFactory _factory;

    private const string StoredImageUrlKey = "StoredImageUrl";
    private const string StoredSlugKey = "StoredSlug";

    public ImageSteps(
        ImageApiDriver imageDriver,
        ApiContext apiContext,
        ScenarioContext scenarioContext,
        TacBlogWebApplicationFactory factory)
    {
        _imageDriver = imageDriver;
        _apiContext = apiContext;
        _scenarioContext = scenarioContext;
        _factory = factory;
    }

    // ── Epic 3: UploadImage (US-030) ──

    [When("Christian uploads an image {string}")]
    public async Task WhenChristianUploadsAnImage(string fileName)
    {
        var fakePngBytes = CreateFakePngBytes();
        await _imageDriver.UploadImage(fileName, fakePngBytes);
    }

    [Then("the response contains a URL for the uploaded image")]
    public void ThenTheResponseContainsAUrlForTheUploadedImage()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var url = _apiContext.LastResponseJson!.RootElement
            .GetProperty("url").GetString();

        url.Should().NotBeNullOrWhiteSpace();
        url.Should().StartWith("https://");
    }

    [When("Christian uploads a file {string} as an image")]
    public async Task WhenChristianUploadsAFileAsAnImage(string fileName)
    {
        var fakeContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        await _imageDriver.UploadNonImageFile(fileName, fakeContent);
    }

    [Given("the image storage service is temporarily unavailable")]
    public void GivenTheImageStorageServiceIsTemporarilyUnavailable()
    {
        var stub = _factory.Services.GetRequiredService<StubImageStorage>();
        stub.ShouldFail = true;
    }

    // ── Epic 3: SetFeaturedImage (US-031) ──

    [Given("an image has been uploaded with URL {string}")]
    public void GivenAnImageHasBeenUploadedWithUrl(string url)
    {
        _scenarioContext[StoredImageUrlKey] = url;
        CaptureSlugFromLastResponse();
    }

    [When("Christian sets the featured image on the post")]
    public async Task WhenChristianSetsTheFeaturedImageOnThePost()
    {
        var slug = (string)_scenarioContext[StoredSlugKey];
        var imageUrl = (string)_scenarioContext[StoredImageUrlKey];
        await _imageDriver.SetFeaturedImage(slug, imageUrl);
    }

    [Then("the post has the featured image URL")]
    public void ThenThePostHasTheFeaturedImageUrl()
    {
        _apiContext.LastResponseJson.Should().NotBeNull();
        var imageUrl = _apiContext.LastResponseJson!.RootElement
            .GetProperty("featuredImageUrl").GetString();

        var storedUrl = (string)_scenarioContext[StoredImageUrlKey];
        imageUrl.Should().Be(storedUrl);
    }

    [When("Christian sets the featured image URL to {string}")]
    public async Task WhenChristianSetsTheFeaturedImageUrlTo(string url)
    {
        CaptureSlugFromLastResponse();
        var slug = (string)_scenarioContext[StoredSlugKey];
        await _imageDriver.SetFeaturedImage(slug, url);
    }

    // ── Epic 3: RemoveFeaturedImage (US-032) ──

    [When("Christian removes the featured image from the post")]
    public async Task WhenChristianRemovesTheFeaturedImageFromThePost()
    {
        CaptureSlugFromLastResponse();
        var slug = (string)_scenarioContext[StoredSlugKey];
        await _imageDriver.RemoveFeaturedImage(slug);
    }

    [When("Christian removes the featured image from a non-existent post")]
    public async Task WhenChristianRemovesTheFeaturedImageFromANonExistentPost()
    {
        await _imageDriver.RemoveFeaturedImage("non-existent-post-slug");
    }

    // ── Helpers ──

    private void CaptureSlugFromLastResponse()
    {
        if (_scenarioContext.ContainsKey(StoredSlugKey))
            return;

        _apiContext.LastResponseJson.Should().NotBeNull();
        var slug = _apiContext.LastResponseJson!.RootElement
            .GetProperty("slug").GetString();

        _scenarioContext[StoredSlugKey] = slug!;
    }

    private static byte[] CreateFakePngBytes()
    {
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    }
}
