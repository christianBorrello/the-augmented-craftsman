using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class PostIdShould
{
    [Fact]
    public void generate_unique_values()
    {
        var first = PostId.NewUnique();
        var second = PostId.NewUnique();

        first.Should().NotBe(second);
    }

    [Fact]
    public void wrap_existing_guid_for_reconstitution()
    {
        var guid = Guid.NewGuid();

        var postId = new PostId(guid);

        postId.Should().Be(new PostId(guid));
    }

    [Fact]
    public void reject_empty_guid()
    {
        var act = () => new PostId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
