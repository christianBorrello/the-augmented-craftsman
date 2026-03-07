using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Tags;

public sealed record DeleteTagResult(bool IsSuccess, bool IsNotFound)
{
    public static DeleteTagResult Success() => new(true, false);
    public static DeleteTagResult NotFound() => new(false, true);
}

public sealed class DeleteTag(ITagRepository repository)
{
    public async Task<DeleteTagResult> ExecuteAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var tag = await repository.FindBySlugAsync(new Slug(slug), cancellationToken);

        if (tag is null)
            return DeleteTagResult.NotFound();

        await repository.DeleteAsync(tag.Id, cancellationToken);

        return DeleteTagResult.Success();
    }
}
