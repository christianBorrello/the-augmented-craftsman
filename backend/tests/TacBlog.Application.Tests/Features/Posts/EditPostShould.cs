using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class EditPostShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OriginalCreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly EditPost _useCase;

    public EditPostShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new EditPost(_repository, _clock);
    }

    [Fact]
    public async Task update_title_and_content_on_existing_post()
    {
        var post = CreateExistingPost("Original Title", "Original content");
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(
            post.Id.Value, "Updated Title", "Updated content");

        result.IsSuccess.Should().BeTrue();
        result.Post!.Title.ToString().Should().Be("Updated Title");
        result.Post.Content.ToString().Should().Be("Updated content");
        result.Post.UpdatedAt.Should().Be(FixedNow);

        await _repository.Received(1).SaveAsync(
            Arg.Is<BlogPost>(p => p.Title.ToString() == "Updated Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task preserve_original_slug_after_title_edit()
    {
        var post = CreateExistingPost("Original Title", "Some content");
        var originalSlug = post.Slug.ToString();
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(
            post.Id.Value, "Completely Different Title", "Some content");

        result.IsSuccess.Should().BeTrue();
        result.Post!.Slug.ToString().Should().Be(originalSlug);
    }

    [Fact]
    public async Task replace_tags_with_new_set()
    {
        var post = CreateExistingPost("Tagged Post", "Content");
        post.AddTag(Tag.Create(new TagName("OldTag")));
        post.AddTag(Tag.Create(new TagName("RemoveMe")));
        SetupRepositoryToReturn(post);

        var newTags = new List<string> { "TDD", "Clean Code" };

        var result = await _useCase.ExecuteAsync(
            post.Id.Value, "Tagged Post", "Content", newTags);

        result.IsSuccess.Should().BeTrue();
        result.Post!.Tags.Should().HaveCount(2);
        result.Post.Tags.Select(t => t.Name.ToString()).Should().BeEquivalentTo("TDD", "Clean Code");
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_post()
    {
        var unknownId = Guid.NewGuid();
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync(unknownId, "Title", "Content");

        result.IsNotFound.Should().BeTrue();
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<BlogPost>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task return_validation_error_for_invalid_title(string? title)
    {
        var post = CreateExistingPost("Valid Title", "Valid content");
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(post.Id.Value, title!, "Valid content");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Title");
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<BlogPost>(),
            Arg.Any<CancellationToken>());
    }

    private BlogPost CreateExistingPost(string title, string content) =>
        BlogPost.Create(new Title(title), new PostContent(content), OriginalCreatedAt);

    private void SetupRepositoryToReturn(BlogPost post) =>
        _repository.FindByIdAsync(Arg.Any<PostId>(), Arg.Any<CancellationToken>())
            .Returns(post);
}
