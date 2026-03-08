using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Likes;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Likes;

public class UnlikePostShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _postRepository = Substitute.For<IBlogPostRepository>();
    private readonly ILikeRepository _likeRepository = Substitute.For<ILikeRepository>();
    private readonly UnlikePost _useCase;

    public UnlikePostShould()
    {
        _useCase = new UnlikePost(_postRepository, _likeRepository);
    }

    [Fact]
    public async Task remove_like_and_return_decremented_count()
    {
        var slug = new Slug("tdd-is-not-about-testing");
        var visitorId = new VisitorId(Guid.NewGuid());

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("TDD Is Not About Testing"), new PostContent("Content"), FixedNow));
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId.Value.ToString());

        result.IsNotFound.Should().BeFalse();
        result.Count.Should().Be(0);
        await _likeRepository.Received(1).DeleteAsync(slug, visitorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_count_zero_when_no_like_exists()
    {
        var slug = new Slug("tdd-is-not-about-testing");
        var visitorId = new VisitorId(Guid.NewGuid());

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("TDD Is Not About Testing"), new PostContent("Content"), FixedNow));
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId.Value.ToString());

        result.IsNotFound.Should().BeFalse();
        result.Count.Should().Be(0);
    }
}
