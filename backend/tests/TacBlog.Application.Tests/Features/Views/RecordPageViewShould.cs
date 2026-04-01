using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Views;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Views;

public class RecordPageViewShould
{
    private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _postRepository = Substitute.For<IBlogPostRepository>();
    private readonly IPageViewRepository _viewRepository = Substitute.For<IPageViewRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly RecordPageView _useCase;

    public RecordPageViewShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new RecordPageView(_postRepository, _viewRepository, _clock);
    }

    [Fact]
    public async Task save_view_and_return_count_when_post_exists()
    {
        var slug = new Slug("my-post");
        var visitorId = Guid.NewGuid().ToString();

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("My Post"), new PostContent("Content"), FixedNow));
        _viewRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(5);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId);

        result.IsNotFound.Should().BeFalse();
        result.Count.Should().Be(5);
        await _viewRepository.Received(1).SaveAsync(Arg.Any<PageView>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_when_post_does_not_exist()
    {
        var slug = new Slug("non-existent");

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(slug.Value, Guid.NewGuid().ToString());

        result.IsNotFound.Should().BeTrue();
        result.Count.Should().Be(0);
        await _viewRepository.DidNotReceive().SaveAsync(Arg.Any<PageView>(), Arg.Any<CancellationToken>());
    }
}
