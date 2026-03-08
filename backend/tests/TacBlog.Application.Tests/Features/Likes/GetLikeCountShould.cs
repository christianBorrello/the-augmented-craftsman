using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Likes;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Likes;

public class GetLikeCountShould
{
    private readonly IBlogPostRepository _postRepository = Substitute.For<IBlogPostRepository>();
    private readonly ILikeRepository _likeRepository = Substitute.For<ILikeRepository>();
    private readonly GetLikeCount _useCase;

    private static readonly DateTime FixedNow = new(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);

    public GetLikeCountShould()
    {
        _useCase = new GetLikeCount(_postRepository, _likeRepository);
    }

    [Fact]
    public async Task return_count_when_post_exists()
    {
        var slug = new Slug("tdd-is-not-about-testing");

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("TDD Is Not About Testing"), new PostContent("Content"), FixedNow));
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(3);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsSuccess.Should().BeTrue();
        result.Count.Should().Be(3);
    }

    [Fact]
    public async Task return_not_found_when_post_does_not_exist()
    {
        var slug = new Slug("non-existent");

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task return_zero_for_post_with_no_likes()
    {
        var slug = new Slug("no-likes-post");

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("No Likes Post"), new PostContent("Content"), FixedNow));
        _likeRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsSuccess.Should().BeTrue();
        result.Count.Should().Be(0);
    }
}
