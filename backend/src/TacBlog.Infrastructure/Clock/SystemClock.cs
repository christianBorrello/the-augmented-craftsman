using TacBlog.Application.Ports.Driven;

namespace TacBlog.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
