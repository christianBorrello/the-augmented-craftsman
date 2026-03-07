using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Tags;

public class DeleteTagShould
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly DeleteTag _useCase;

    public DeleteTagShould()
    {
        _useCase = new DeleteTag(_repository);
    }

    [Fact]
    public async Task remove_existing_tag_from_repository()
    {
        var tag = Tag.Create(new TagName("TDD"));
        var slug = Slug.FromTagName(new TagName("TDD"));
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(tag);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsSuccess.Should().BeTrue();
        result.IsNotFound.Should().BeFalse();

        await _repository.Received(1).DeleteAsync(
            Arg.Any<TagId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_not_found_for_nonexistent_tag_slug()
    {
        var slug = Slug.FromTagName(new TagName("nonexistent"));
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns((Tag?)null);

        var result = await _useCase.ExecuteAsync(slug.Value);

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();

        await _repository.DidNotReceive().DeleteAsync(
            Arg.Any<TagId>(),
            Arg.Any<CancellationToken>());
    }
}
