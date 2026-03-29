using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Tags;

public sealed record PublicTagResult(string Name, string Slug, string Color, string ColorDark, int PostCount);
public sealed record BrowsePublicTagsResult(IReadOnlyList<PublicTagResult> Tags);

public sealed class BrowsePublicTags(ITagRepository repository)
{
    public async Task<BrowsePublicTagsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var allTags = await repository.GetPublicTagsWithPostCountsAsync(cancellationToken);

        var publicTags = allTags
            .Where(tagWithCount => tagWithCount.PostCount > 0)
            .OrderBy(tagWithCount => tagWithCount.Tag.Name.ToString())
            .Select(tagWithCount => new PublicTagResult(
                tagWithCount.Tag.Name.Value,
                tagWithCount.Tag.Slug.Value,
                tagWithCount.Tag.Color.Light,
                tagWithCount.Tag.Color.Dark,
                tagWithCount.PostCount))
            .ToList();

        return new BrowsePublicTagsResult(publicTags);
    }
}
