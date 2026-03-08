using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface IReaderSessionRepository
{
    Task SaveAsync(ReaderSession session, CancellationToken cancellationToken);
    Task<ReaderSession?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken);
}
