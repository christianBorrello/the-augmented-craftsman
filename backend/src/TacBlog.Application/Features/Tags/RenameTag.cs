using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;

namespace TacBlog.Application.Features.Tags;

public sealed record RenameTagResult(bool IsSuccess, bool IsNotFound, bool IsConflict, Tag? Tag, string? ErrorMessage)
{
    public static RenameTagResult Success(Tag tag) => new(true, false, false, tag, null);
    public static RenameTagResult NotFound() => new(false, true, false, null, null);
    public static RenameTagResult Conflict(string message) => new(false, false, true, null, message);
    public static RenameTagResult ValidationError(string message) => new(false, false, false, null, message);
}

public sealed class RenameTag(ITagRepository repository)
{
    public async Task<RenameTagResult> ExecuteAsync(
        string currentSlug,
        string newName,
        CancellationToken cancellationToken = default)
    {
        TagName validatedName;
        try
        {
            validatedName = new TagName(newName);
        }
        catch (ArgumentException exception)
        {
            return RenameTagResult.ValidationError(exception.Message);
        }

        var tag = await repository.FindBySlugAsync(new Slug(currentSlug), cancellationToken);

        if (tag is null)
            return RenameTagResult.NotFound();

        var newSlug = Slug.FromTagName(validatedName);

        if (await repository.ExistsBySlugAsync(newSlug, cancellationToken))
            return RenameTagResult.Conflict($"A tag named '{validatedName}' already exists");

        tag.Rename(validatedName);

        await repository.SaveAsync(tag, cancellationToken);

        return RenameTagResult.Success(tag);
    }
}
