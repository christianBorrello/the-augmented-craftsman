using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Tags;

public class ListTagsShould
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly ListTags _useCase;

    public ListTagsShould()
    {
        _useCase = new ListTags(_repository);
    }

    [Fact]
    public async Task return_all_tags_sorted_alphabetically_with_post_counts()
    {
        var csharp = Tag.Create(new TagName("C#"));
        var architecture = Tag.Create(new TagName("Architecture"));
        var tdd = Tag.Create(new TagName("TDD"));

        _repository.GetAllWithPostCountsAsync(Arg.Any<CancellationToken>())
            .Returns([
                new TagWithPostCount(csharp, 5),
                new TagWithPostCount(architecture, 12),
                new TagWithPostCount(tdd, 3)
            ]);

        var result = await _useCase.ExecuteAsync();

        result.Tags.Should().HaveCount(3);
        result.Tags[0].Tag.Name.ToString().Should().Be("Architecture");
        result.Tags[0].PostCount.Should().Be(12);
        result.Tags[1].Tag.Name.ToString().Should().Be("C#");
        result.Tags[1].PostCount.Should().Be(5);
        result.Tags[2].Tag.Name.ToString().Should().Be("TDD");
        result.Tags[2].PostCount.Should().Be(3);
    }

    [Fact]
    public async Task return_empty_list_when_no_tags_exist()
    {
        _repository.GetAllWithPostCountsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TagWithPostCount>());

        var result = await _useCase.ExecuteAsync();

        result.Tags.Should().BeEmpty();
    }
}
