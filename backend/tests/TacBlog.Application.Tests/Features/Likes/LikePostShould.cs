using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Likes;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Likes;

public class LikePostShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _postRepository = Substitute.For<IBlogPostRepository>();
    private readonly ILikeRepository _likeRepository = Substitute.For<ILikeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly LikePost _useCase;

    public LikePostShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new LikePost(_postRepository, _likeRepository, _clock);
    }

    [Fact]
    public async Task return_success_with_count_when_post_exists()
    {
        var slug = new Slug("tdd-is-not-about-testing");
        var visitorId = new VisitorId(Guid.NewGuid());

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("TDD Is Not About Testing"), new PostContent("Content"), FixedNow));
        _likeRepository.ExistsAsync(slug, visitorId, Arg.Any<CancellationToken>())
            .Returns(false);
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId.Value.ToString());

        result.IsSuccess.Should().BeTrue();
        result.Count.Should().Be(1);
        await _likeRepository.Received(1).SaveAsync(Arg.Any<Like>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_when_post_does_not_exist()
    {
        var slug = new Slug("non-existent-post");
        var visitorId = Guid.NewGuid().ToString();

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId);

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        await _likeRepository.DidNotReceive().SaveAsync(Arg.Any<Like>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_success_when_already_liked()
    {
        var slug = new Slug("already-liked-post");
        var visitorId = new VisitorId(Guid.NewGuid());

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("Already Liked Post"), new PostContent("Content"), FixedNow));
        _likeRepository.ExistsAsync(slug, visitorId, Arg.Any<CancellationToken>())
            .Returns(true);
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(5);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId.Value.ToString());

        result.IsSuccess.Should().BeTrue();
        result.Count.Should().Be(5);
        await _likeRepository.DidNotReceive().SaveAsync(Arg.Any<Like>(), Arg.Any<CancellationToken>());
    }
}
