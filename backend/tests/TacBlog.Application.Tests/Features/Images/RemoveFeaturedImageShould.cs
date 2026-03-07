using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Images;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Images;

public class RemoveFeaturedImageShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OriginalCreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly RemoveFeaturedImage _useCase;

    public RemoveFeaturedImageShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new RemoveFeaturedImage(_repository, _clock);
    }

    [Fact]
    public async Task clear_featured_image_on_existing_post()
    {
        var post = CreateExistingPostWithFeaturedImage();
        SetupRepositoryToReturn(post);

        var result = await _useCase.ExecuteAsync(post.Slug.ToString());

        result.IsSuccess.Should().BeTrue();
        result.Post!.FeaturedImageUrl.Should().BeNull();
        result.Post.UpdatedAt.Should().Be(FixedNow);

        await _repository.Received(1).SaveAsync(post, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_post()
    {
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync("nonexistent-post");

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Post.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
    }

    private BlogPost CreateExistingPostWithFeaturedImage()
    {
        var post = BlogPost.Create(new Title("My Post"), new PostContent("Content"), OriginalCreatedAt);
        post.SetFeaturedImage(new FeaturedImageUrl("https://ik.imagekit.io/blog/image.jpg"), OriginalCreatedAt);
        return post;
    }

    private void SetupRepositoryToReturn(BlogPost post) =>
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(post);
}
