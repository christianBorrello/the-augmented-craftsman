using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class PreviewPostShould
{
    private static readonly DateTime OriginalCreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly PreviewPost _useCase;

    public PreviewPostShould()
    {
        _useCase = new PreviewPost(_repository);
    }

    [Fact]
    public async Task return_post_data_for_preview()
    {
        var post = BlogPost.Create(new Title("My Post"), new PostContent("Content with ```code blocks```"), OriginalCreatedAt);
        post.AddTag(Tag.Create(new TagName("csharp")));
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns(post);

        var result = await _useCase.ExecuteAsync(post.Id.Value);

        result.IsSuccess.Should().BeTrue();
        result.IsNotFound.Should().BeFalse();
        result.Post.Should().NotBeNull();
        result.Post!.Content.Value.Should().Contain("```code blocks```");
        result.Post.Tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_post()
    {
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();
    }
}
