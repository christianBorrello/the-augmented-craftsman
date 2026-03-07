using TacBlog.Application.Ports.Driven;

namespace TacBlog.Acceptance.Tests.Support;

public sealed class StubImageStorage : IImageStorage
{
    public bool ShouldFail { get; set; }

    public Task<string> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        if (ShouldFail)
            throw new ImageStorageException("Storage unavailable");

        return Task.FromResult($"https://ik.imagekit.io/test/{fileName}");
    }
}
