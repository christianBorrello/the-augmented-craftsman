using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Tags;

public sealed record CreateTagResult(bool IsSuccess, bool IsConflict, Tag? Tag, string? ErrorMessage)
{
    public static CreateTagResult Success(Tag tag) => new(true, false, tag, null);
    public static CreateTagResult ValidationError(string message) => new(false, false, null, message);
    public static CreateTagResult Conflict(string message) => new(false, true, null, message);
}

public sealed class CreateTag(ITagRepository repository)
{
    public async Task<CreateTagResult> ExecuteAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        TagName validatedName;
        try
        {
            validatedName = new TagName(name);
        }
        catch (ArgumentException exception)
        {
            return CreateTagResult.ValidationError(exception.Message);
        }

        var tag = Tag.Create(validatedName);

        if (await repository.ExistsBySlugAsync(tag.Slug, cancellationToken))
            return CreateTagResult.Conflict("A tag with this URL already exists");

        await repository.SaveAsync(tag, cancellationToken);

        return CreateTagResult.Success(tag);
    }
}
