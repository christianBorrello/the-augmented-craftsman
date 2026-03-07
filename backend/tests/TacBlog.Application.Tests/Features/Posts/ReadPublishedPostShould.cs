using Xunit;
using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Tests.Features.Posts;

public class ReadPublishedPostShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly ReadPublishedPost _useCase;

    public ReadPublishedPostShould()
    {
        _useCase = new ReadPublishedPost(_repository);
    }

    [Fact]
    public async Task return_post_when_slug_exists_and_post_is_published()
    {
        var post = BlogPost.Create(new Title("My Published Post"), new PostContent("Content"), FixedNow);
        post.Publish(FixedNow);
        _repository.FindBySlugAsync(Arg.Is<Slug>(s => s.Value == "my-published-post"), Arg.Any<CancellationToken>())
            .Returns(post);

        var result = await _useCase.ExecuteAsync("my-published-post");

        result.IsSuccess.Should().BeTrue();
        result.Post.Should().BeSameAs(post);
    }

    [Fact]
    public async Task return_not_found_when_post_is_draft()
    {
        var draftPost = BlogPost.Create(new Title("Draft Post"), new PostContent("Content"), FixedNow);
        _repository.FindBySlugAsync(Arg.Is<Slug>(s => s.Value == "draft-post"), Arg.Any<CancellationToken>())
            .Returns(draftPost);

        var result = await _useCase.ExecuteAsync("draft-post");

        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();
    }

    [Fact]
    public async Task return_not_found_when_slug_does_not_exist()
    {
        _repository.FindBySlugAsync(Arg.Is<Slug>(s => s.Value == "non-existent-slug"), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync("non-existent-slug");

        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();
    }

    [Fact]
    public async Task return_not_found_for_invalid_slug()
    {
        var result = await _useCase.ExecuteAsync("");

        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();
    }
}
