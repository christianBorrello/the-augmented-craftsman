using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Likes;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Likes;

public class CheckIfLikedShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _postRepository = Substitute.For<IBlogPostRepository>();
    private readonly ILikeRepository _likeRepository = Substitute.For<ILikeRepository>();
    private readonly CheckIfLiked _useCase;

    public CheckIfLikedShould()
    {
        _useCase = new CheckIfLiked(_postRepository, _likeRepository);
    }

    [Fact]
    public async Task return_liked_true_with_count_when_visitor_has_liked()
    {
        var slug = new Slug("tdd-is-not-about-testing");
        var visitorId = new VisitorId(Guid.NewGuid());

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("TDD Is Not About Testing"), new PostContent("Content"), FixedNow));
        _likeRepository.ExistsAsync(slug, visitorId, Arg.Any<CancellationToken>())
            .Returns(true);
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(3);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId.Value.ToString());

        result.IsLiked.Should().BeTrue();
        result.Count.Should().Be(3);
        result.IsNotFound.Should().BeFalse();
    }

    [Fact]
    public async Task return_liked_false_with_count_when_visitor_has_not_liked()
    {
        var slug = new Slug("tdd-is-not-about-testing");
        var visitorId = new VisitorId(Guid.NewGuid());

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("TDD Is Not About Testing"), new PostContent("Content"), FixedNow));
        _likeRepository.ExistsAsync(slug, visitorId, Arg.Any<CancellationToken>())
            .Returns(false);
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(5);

        var result = await _useCase.ExecuteAsync(slug.Value, visitorId.Value.ToString());

        result.IsLiked.Should().BeFalse();
        result.Count.Should().Be(5);
        result.IsNotFound.Should().BeFalse();
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
    }
}
