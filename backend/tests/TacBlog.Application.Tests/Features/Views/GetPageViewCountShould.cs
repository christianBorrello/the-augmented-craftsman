using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Views;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Views;

public class GetPageViewCountShould
{
    private readonly IBlogPostRepository _postRepository = Substitute.For<IBlogPostRepository>();
    private readonly IPageViewRepository _viewRepository = Substitute.For<IPageViewRepository>();
    private readonly GetPageViewCount _useCase;

    public GetPageViewCountShould()
    {
        _useCase = new GetPageViewCount(_postRepository, _viewRepository);
    }

    [Fact]
    public async Task return_count_when_post_exists()
    {
        var slug = new Slug("my-post");

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(BlogPost.Create(new Title("My Post"), new PostContent("Content"), DateTime.UtcNow));
        _viewRepository.CountBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(42);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsNotFound.Should().BeFalse();
        result.Count.Should().Be(42);
    }

    [Fact]
    public async Task return_not_found_when_post_does_not_exist()
    {
        var slug = new Slug("non-existent");

        _postRepository.FindBySlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsNotFound.Should().BeTrue();
        result.Count.Should().Be(0);
    }
}
