using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class TitleShould
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void reject_empty_or_whitespace_input(string? input)
    {
        var act = () => new Title(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void trim_whitespace_from_input()
    {
        var title = new Title("  Clean Architecture  ");

        title.Should().Be(new Title("Clean Architecture"));
    }

    [Fact]
    public void reject_input_exceeding_200_characters()
    {
        var longInput = new string('a', 201);

        var act = () => new Title(longInput);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void accept_valid_input_up_to_200_characters()
    {
        var validInput = new string('a', 200);

        var title = new Title(validInput);

        title.ToString().Should().Be(validInput);
    }
}
