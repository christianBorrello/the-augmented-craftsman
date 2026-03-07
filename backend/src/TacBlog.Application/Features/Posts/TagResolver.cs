using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Posts;

internal static class TagResolver
{
    internal static async Task<IReadOnlyList<Tag>> ResolveAsync(
        IReadOnlyList<string> tagNames,
        IBlogPostRepository repository,
        CancellationToken cancellationToken)
    {
        var tags = new List<Tag>(tagNames.Count);

        foreach (var name in tagNames)
        {
            var tagName = new TagName(name);
            var slug = Slug.FromTagName(tagName);
            var existingTag = await repository.FindTagBySlugAsync(slug, cancellationToken);
            tags.Add(existingTag ?? Tag.Create(tagName));
        }

        return tags;
    }
}
