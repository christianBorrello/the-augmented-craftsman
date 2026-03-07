using Imagekit.Sdk;
using TacBlog.Application.Ports.Driven;

namespace TacBlog.Infrastructure.Storage;

public sealed class ImageKitImageStorage(ImageKitSettings settings) : IImageStorage
{
    public async Task<string> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var imagekit = new ImagekitClient(settings.PublicKey, settings.PrivateKey, settings.UrlEndpoint);
            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream, cancellationToken);

            var request = new FileCreateRequest
            {
                file = memoryStream.ToArray(),
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
