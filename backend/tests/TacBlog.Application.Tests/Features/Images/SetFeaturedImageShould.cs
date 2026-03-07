using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Images;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Images;

public class SetFeaturedImageShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OriginalCreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidImageUrl = "https://ik.imagekit.io/blog/featured.jpg";

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly SetFeaturedImage _useCase;

    public SetFeaturedImageShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new SetFeaturedImage(_repository, _clock);
    }

    [Fact]
    public async Task persist_featured_image_on_existing_post()
    {
        var post = CreateExistingPost();
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(post.Slug.ToString(), ValidImageUrl);

        result.IsSuccess.Should().BeTrue();
        result.Post!.FeaturedImageUrl.Should().NotBeNull();
        result.Post.FeaturedImageUrl!.Value.Value.Should().Be(ValidImageUrl);
        result.Post.UpdatedAt.Should().Be(FixedNow);

        await _repository.Received(1).SaveAsync(post, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_validation_error_for_invalid_url()
    {
        var post = CreateExistingPost();
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(post.Slug.ToString(), "not-a-valid-url");

        result.IsSuccess.Should().BeFalse();
        result.IsValidationError.Should().BeTrue();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_post()
    {
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync("nonexistent-post", ValidImageUrl);

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
    }

    private BlogPost CreateExistingPost() =>
        BlogPost.Create(new Title("My Post"), new PostContent("Content"), OriginalCreatedAt);

    private void SetupRepositoryToReturn(BlogPost post) =>
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(post);
}
