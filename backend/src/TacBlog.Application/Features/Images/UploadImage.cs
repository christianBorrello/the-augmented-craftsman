using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Images;

public sealed record UploadImageResult(
    bool IsSuccess,
    bool IsServiceUnavailable,
    string? Url,
    string? ErrorMessage)
{
    public static UploadImageResult Success(string url) => new(true, false, url, null);
    public static UploadImageResult ValidationError(string message) => new(false, false, null, message);
    public static UploadImageResult ServiceUnavailable(string message) => new(false, true, null, message);
}

public sealed class UploadImage(IImageStorage imageStorage)
{
    public async Task<UploadImageResult> ExecuteAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return UploadImageResult.ValidationError(
                $"Content type '{contentType}' is not a supported image format");

        try
        {
            var url = await imageStorage.UploadAsync(content, fileName, cancellationToken);
            return UploadImageResult.Success(url);
        }
        catch (ImageStorageException exception)
        {
            return UploadImageResult.ServiceUnavailable(exception.Message);
        }
    }
}
