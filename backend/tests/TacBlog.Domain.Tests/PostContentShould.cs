using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class PostContentShould
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void reject_empty_or_whitespace_input(string? input)
    {
        var act = () => new PostContent(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void preserve_raw_markdown_including_code_blocks()
    {
        var markdown = """
                       # Hello World

                       Some text with **bold** and `inline code`.

                       ```csharp
                       public class Foo
                       {
                           public void Bar() { }
                       }
                       ```
                       """;

        var content = new PostContent(markdown);

        content.ToString().Should().Be(markdown);
    }
}
