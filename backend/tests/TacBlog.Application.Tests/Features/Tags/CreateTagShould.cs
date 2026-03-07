using Xunit;
using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Tests.Features.Tags;

public class CreateTagShould
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly CreateTag _useCase;

    public CreateTagShould()
    {
        _useCase = new CreateTag(_repository);
    }

    [Fact]
    public async Task persist_tag_with_generated_slug_for_valid_name()
    {
        var result = await _useCase.ExecuteAsync("Clean Code");

        result.IsSuccess.Should().BeTrue();
        result.Tag.Should().NotBeNull();
        result.Tag!.Name.ToString().Should().Be("Clean Code");
        result.Tag.Slug.ToString().Should().Be("clean-code");

        await _repository.Received(1).SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_conflict_when_slug_already_exists()
    {
        _repository.ExistsBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _useCase.ExecuteAsync("Duplicate Tag");

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeTrue();
        result.ErrorMessage.Should().Contain("already exists");
        result.Tag.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_validation_error_for_invalid_name()
    {
        var result = await _useCase.ExecuteAsync("");

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Tag.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }
}
