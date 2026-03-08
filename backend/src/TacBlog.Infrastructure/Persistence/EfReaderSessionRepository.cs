using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Infrastructure.Persistence;

public sealed class EfReaderSessionRepository(TacBlogDbContext context) : IReaderSessionRepository
{
    public async Task SaveAsync(ReaderSession session, CancellationToken cancellationToken)
    {
        context.ReaderSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReaderSession?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await context.ReaderSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await context.ReaderSessions.FirstOrDefaultAsync(
            s => s.Id == sessionId, cancellationToken);

        if (session is not null)
        {
            context.ReaderSessions.Remove(session);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
