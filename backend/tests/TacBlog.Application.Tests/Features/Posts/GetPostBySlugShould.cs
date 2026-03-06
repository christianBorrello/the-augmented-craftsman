using Xunit;
using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Tests.Features.Posts;

public class GetPostBySlugShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly GetPostBySlug _useCase;

    public GetPostBySlugShould()
    {
        _useCase = new GetPostBySlug(_repository);
    }

    [Fact]
    public async Task return_post_when_slug_exists()
    {
        var post = BlogPost.Create(new Title("My First Post"), new PostContent("Some content"), FixedNow);
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(post);

        var result = await _useCase.ExecuteAsync("my-first-post");

        result.IsSuccess.Should().BeTrue();
        result.Post.Should().BeSameAs(post);
        await _repository.Received(1).FindBySlugAsync(
            Arg.Is<Slug>(s => s.ToString() == "my-first-post"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_when_slug_does_not_exist()
    {
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync("non-existent-slug");

        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task return_not_found_for_invalid_slug(string? slug)
    {
        var result = await _useCase.ExecuteAsync(slug!);

        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();
        await _repository.DidNotReceive().FindBySlugAsync(
            Arg.Any<Slug>(),
            Arg.Any<CancellationToken>());
    }
}
