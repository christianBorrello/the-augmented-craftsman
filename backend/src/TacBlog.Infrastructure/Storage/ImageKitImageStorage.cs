using Imagekit.Sdk;
using TacBlog.Application.Ports.Driven;

namespace TacBlog.Infrastructure.Storage;

public sealed class ImageKitImageStorage : IImageStorage
{
    private readonly ImageKitSettings _settings;

    public ImageKitImageStorage(ImageKitSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var imagekit = new ImagekitClient(_settings.PublicKey, _settings.PrivateKey, _settings.UrlEndpoint);
            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            var request = new FileCreateRequest
            {
                file = fileBytes,
                fileName = fileName,
                useUniqueFileName = true
            };

            var result = await imagekit.UploadAsync(request);
            return result.url;
        }
        catch (Exception exception) when (exception is not ImageStorageException)
        {
            throw new ImageStorageException(
                $"Failed to upload image '{fileName}' to ImageKit", exception);
        }
    }
}
