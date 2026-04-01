using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Views;

public sealed record GetPageViewCountResult(bool IsNotFound, int Count)
{
    public static GetPageViewCountResult Success(int count) => new(false, count);
    public static GetPageViewCountResult NotFound() => new(true, 0);
}

public sealed class GetPageViewCount(IBlogPostRepository postRepository, IPageViewRepository viewRepository)
{
    public async Task<GetPageViewCountResult> ExecuteAsync(
        string slugValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return GetPageViewCountResult.NotFound();

        var count = await viewRepository.CountBySlugAsync(slug, cancellationToken);
        return GetPageViewCountResult.Success(count);
    }
}
