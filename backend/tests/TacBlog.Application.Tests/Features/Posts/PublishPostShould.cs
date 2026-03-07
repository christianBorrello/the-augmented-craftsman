using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class PublishPostShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 7, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OriginalCreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly PublishPost _useCase;

    public PublishPostShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new PublishPost(_repository, _clock);
    }

    [Fact]
    public async Task transition_draft_post_to_published_with_timestamp()
    {
        var post = BlogPost.Create(new Title("My Draft"), new PostContent("Draft content"), OriginalCreatedAt);
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(post.Id.Value);

        result.IsSuccess.Should().BeTrue();
        result.Post!.Status.Should().Be(PostStatus.Published);
        result.Post.PublishedAt.Should().Be(FixedNow);

        await _repository.Received(1).SaveAsync(
            Arg.Is<BlogPost>(p => p.Status == PostStatus.Published),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_post()
    {
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.IsNotFound.Should().BeTrue();
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<BlogPost>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_conflict_for_already_published_post()
    {
        var post = BlogPost.Create(new Title("Published Post"), new PostContent("Content"), OriginalCreatedAt);
        post.Publish(OriginalCreatedAt);
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(post.Id.Value);

        result.IsConflict.Should().BeTrue();
        result.ErrorMessage.Should().Contain("already published");
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<BlogPost>(),
            Arg.Any<CancellationToken>());
    }

    private void SetupRepositoryToReturn(BlogPost post) =>
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns(post);
}
