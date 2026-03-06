using Xunit;
using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Tests.Features.Posts;

public class CreatePostShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreatePost _useCase;

    public CreatePostShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new CreatePost(_repository, _clock);
    }

    [Fact]
    public async Task persist_post_with_generated_slug_for_valid_input()
    {
        var result = await _useCase.ExecuteAsync("My First Post", "Some interesting content");

        result.IsSuccess.Should().BeTrue();
        result.Post.Should().NotBeNull();
        result.Post!.Title.ToString().Should().Be("My First Post");
        result.Post.Slug.ToString().Should().Be("my-first-post");
        result.Post.Content.ToString().Should().Be("Some interesting content");
        result.Post.Status.Should().Be(PostStatus.Draft);
        result.Post.CreatedAt.Should().Be(FixedNow);

        await _repository.Received(1).SaveAsync(
            Arg.Is<BlogPost>(p => p.Title.ToString() == "My First Post"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task return_validation_error_for_invalid_title(string? title)
    {
        var result = await _useCase.ExecuteAsync(title!, "Valid content");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Title");
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<BlogPost>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task return_validation_error_for_invalid_content(string? content)
    {
        var result = await _useCase.ExecuteAsync("Valid Title", content!);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Content");
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<BlogPost>(),
            Arg.Any<CancellationToken>());
    }
}
