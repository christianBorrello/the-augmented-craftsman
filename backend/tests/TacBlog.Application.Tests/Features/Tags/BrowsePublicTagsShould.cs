using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Tags;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Tags;

public class BrowsePublicTagsShould
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly BrowsePublicTags _useCase;

    public BrowsePublicTagsShould()
    {
        _useCase = new BrowsePublicTags(_repository);
    }

    [Fact]
    public async Task return_tags_with_post_counts_sorted_alphabetically_excluding_zero_count()
    {
        var tdd = Tag.Create(new TagName("TDD"));
        var architecture = Tag.Create(new TagName("Architecture"));
        var csharp = Tag.Create(new TagName("C#"));
        var unused = Tag.Create(new TagName("Unused"));

        _repository.GetAllWithPostCountsAsync(Arg.Any<CancellationToken>())
            .Returns([
                new TagWithPostCount(tdd, 3),
                new TagWithPostCount(architecture, 12),
                new TagWithPostCount(csharp, 5),
                new TagWithPostCount(unused, 0)
            ]);

        var result = await _useCase.ExecuteAsync();

        result.Tags.Should().HaveCount(3);
        result.Tags[0].Should().Be(new PublicTagResult("Architecture", "architecture", 12));
        result.Tags[1].Should().Be(new PublicTagResult("C#", "c", 5));
        result.Tags[2].Should().Be(new PublicTagResult("TDD", "tdd", 3));
    }

    [Fact]
    public async Task return_empty_list_when_no_tags_have_published_posts()
    {
        var orphan = Tag.Create(new TagName("Orphan"));

        _repository.GetAllWithPostCountsAsync(Arg.Any<CancellationToken>())
            .Returns([new TagWithPostCount(orphan, 0)]);

        var result = await _useCase.ExecuteAsync();

        result.Tags.Should().BeEmpty();
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
