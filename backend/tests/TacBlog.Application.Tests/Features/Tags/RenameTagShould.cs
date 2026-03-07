using Xunit;
using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Tests.Features.Tags;

public class RenameTagShould
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly RenameTag _useCase;

    public RenameTagShould()
    {
        _useCase = new RenameTag(_repository);
    }

    [Fact]
    public async Task update_tag_name_and_slug_for_valid_rename()
    {
        var existingTag = Tag.Create(new TagName("TDD"));
        _repository.FindBySlugAsync(Arg.Is<Slug>(s => s.ToString() == "tdd"), Arg.Any<CancellationToken>())
            .Returns(existingTag);
        _repository.ExistsBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _useCase.ExecuteAsync("tdd", "Test-Driven Development");

        result.IsSuccess.Should().BeTrue();
        result.Tag.Should().NotBeNull();
        result.Tag!.Name.ToString().Should().Be("Test-Driven Development");
        result.Tag.Slug.ToString().Should().Be("test-driven-development");

        await _repository.Received(1).SaveAsync(
            Arg.Is<Tag>(t => t.Name.ToString() == "Test-Driven Development"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_when_source_tag_does_not_exist()
    {
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns((Tag?)null);

        var result = await _useCase.ExecuteAsync("nonexistent", "New Name");

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_conflict_when_new_slug_already_exists()
    {
        var existingTag = Tag.Create(new TagName("TDD"));
        _repository.FindBySlugAsync(Arg.Is<Slug>(s => s.ToString() == "tdd"), Arg.Any<CancellationToken>())
            .Returns(existingTag);
        _repository.ExistsBySlugAsync(Arg.Is<Slug>(s => s.ToString() == "clean-code"), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _useCase.ExecuteAsync("tdd", "Clean Code");

        result.IsConflict.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task return_validation_error_for_invalid_new_name(string? newName)
    {
        var result = await _useCase.ExecuteAsync("tdd", newName!);

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<Tag>(),
            Arg.Any<CancellationToken>());
    }
}
