using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Tags;

public sealed record ListTagsResult(IReadOnlyList<TagWithPostCount> Tags);

public sealed class ListTags(ITagRepository repository)
{
    public async Task<ListTagsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var tags = await repository.GetAllWithPostCountsAsync(cancellationToken);
        var sorted = tags.OrderBy(tagWithCount => tagWithCount.Tag.Name.ToString()).ToList();
        return new ListTagsResult(sorted);
    }
}
