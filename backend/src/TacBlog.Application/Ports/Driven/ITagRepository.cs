using TacBlog.Domain;

namespace TacBlog.Application.Ports.Driven;

public interface ITagRepository
{
    Task<Tag?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<Tag?> FindByIdAsync(TagId id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<TagWithPostCount>> GetAllWithPostCountsAsync(CancellationToken cancellationToken);
    Task SaveAsync(Tag tag, CancellationToken cancellationToken);
    Task DeleteAsync(TagId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TagWithPostCount>> GetPublicTagsWithPostCountsAsync(CancellationToken cancellationToken);
}

public sealed record TagWithPostCount(Tag Tag, int PostCount);
