using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class DeletePostShould
{
    private static readonly DateTime OriginalCreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly DeletePost _useCase;

    public DeletePostShould()
    {
        _useCase = new DeletePost(_repository);
    }

    [Fact]
    public async Task remove_existing_post_from_repository()
    {
        var post = BlogPost.Create(new Title("Post to Delete"), new PostContent("Some content"), OriginalCreatedAt);
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns(post);

        var result = await _useCase.ExecuteAsync(post.Id.Value);

        result.IsSuccess.Should().BeTrue();
        result.IsNotFound.Should().BeFalse();

        await _repository.Received(1).DeleteAsync(
            Arg.Is<PostId>(id => id.Value == post.Id.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_post()
    {
        var unknownId = Guid.NewGuid();
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(unknownId);

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();

        await _repository.DidNotReceive().DeleteAsync(
            Arg.Any<PostId>(),
            Arg.Any<CancellationToken>());
    }
}
