namespace TacBlog.Application.Ports.Driven;

public interface IImageStorage
{
    Task<string> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken);
}
