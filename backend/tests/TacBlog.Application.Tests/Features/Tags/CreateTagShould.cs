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
            Arg.Is<Tag>(t => t.Name.ToString() == "Clean Code"),
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

    [Theory]
    [InlineData("", "empty")]
    [InlineData("   ", "empty")]
    [InlineData(null, "empty")]
    public async Task return_validation_error_for_invalid_name(string? name, string expectedFragment)
    {
        var result = await _useCase.ExecuteAsync(name!);

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Tag.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_validation_error_when_name_exceeds_max_length()
    {
        var longName = new string('a', 51);

        var result = await _useCase.ExecuteAsync(longName);

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeFalse();
        result.ErrorMessage.Should().Contain("50");
        result.Tag.Should().BeNull();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }
}
