namespace TacBlog.Application.Ports.Driven;

public interface IClock
{
    DateTime UtcNow { get; }
}
