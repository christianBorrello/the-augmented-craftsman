using Xunit;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TacBlog.Application.Features.Images;
using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Tests.Features.Images;

public class UploadImageShould
{
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly UploadImage _useCase;

    public UploadImageShould()
    {
        _useCase = new UploadImage(_imageStorage);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public async Task return_success_with_url_for_valid_image_content_type(string contentType)
    {
        var expectedUrl = "https://ik.imagekit.io/blog/photo.png";
        using var stream = new MemoryStream([0x89, 0x50]);
        _imageStorage.UploadAsync(stream, "photo.png", Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        var result = await _useCase.ExecuteAsync(stream, "photo.png", contentType);

        result.IsSuccess.Should().BeTrue();
        result.Url.Should().Be(expectedUrl);
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    [InlineData("application/octet-stream")]
    public async Task return_validation_error_for_non_image_content_type(string contentType)
    {
        using var stream = new MemoryStream([0x00]);

        var result = await _useCase.ExecuteAsync(stream, "file.pdf", contentType);

        result.IsSuccess.Should().BeFalse();
        result.IsServiceUnavailable.Should().BeFalse();
        result.ErrorMessage.Should().Contain("image");
        result.Url.Should().BeNull();
        await _imageStorage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_service_unavailable_when_storage_fails()
    {
        using var stream = new MemoryStream([0x89, 0x50]);
        _imageStorage.UploadAsync(stream, "photo.png", Arg.Any<CancellationToken>())
            .ThrowsAsync(new ImageStorageException("Connection refused"));

        var result = await _useCase.ExecuteAsync(stream, "photo.png", "image/png");

        result.IsSuccess.Should().BeFalse();
        result.IsServiceUnavailable.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Connection refused");
        result.Url.Should().BeNull();
    }

    [Fact]
    public async Task delegate_to_image_storage_exactly_once()
    {
        using var stream = new MemoryStream([0x89, 0x50]);
        _imageStorage.UploadAsync(stream, "photo.png", Arg.Any<CancellationToken>())
            .Returns("https://ik.imagekit.io/blog/photo.png");

        await _useCase.ExecuteAsync(stream, "photo.png", "image/png");

        await _imageStorage.Received(1).UploadAsync(
            stream, "photo.png", Arg.Any<CancellationToken>());
    }
}
