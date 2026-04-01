using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Views;

public sealed record RecordPageViewResult(bool IsNotFound, int Count)
{
    public static RecordPageViewResult Success(int count) => new(false, count);
    public static RecordPageViewResult NotFound() => new(true, 0);
}

public sealed class RecordPageView(IBlogPostRepository postRepository, IPageViewRepository viewRepository, IClock clock)
{
    public async Task<RecordPageViewResult> ExecuteAsync(
        string slugValue,
        string visitorIdValue,
        CancellationToken cancellationToken = default)
    {
        var slug = new Slug(slugValue);

        var post = await postRepository.FindBySlugAsync(slug, cancellationToken);
        if (post is null)
            return RecordPageViewResult.NotFound();

        var visitorId = new VisitorId(Guid.Parse(visitorIdValue));
        var pageView = PageView.Create(slug, visitorId, clock.UtcNow);
        await viewRepository.SaveAsync(pageView, cancellationToken);

        var count = await viewRepository.CountBySlugAsync(slug, cancellationToken);
        return RecordPageViewResult.Success(count);
    }
}
