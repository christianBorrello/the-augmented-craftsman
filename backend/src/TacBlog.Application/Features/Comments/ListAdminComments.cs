using TacBlog.Application.Ports.Driven;

namespace TacBlog.Application.Features.Comments;

public sealed record AdminCommentDto(
    Guid Id,
    string PostSlug,
    string DisplayName,
    string? AvatarUrl,
    string Provider,
    string Text,
    DateTime CreatedAt);

public sealed class ListAdminComments(
    ICommentRepository commentRepository)
{
    public async Task<IReadOnlyList<AdminCommentDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var comments = await commentRepository.FindAllAsync(cancellationToken);

        return comments.Select(c => new AdminCommentDto(
            c.Id.Value,
            c.PostSlug.Value,
            c.DisplayName,
            c.AvatarUrl,
            c.Provider.ToString(),
            c.Text.Value,
            c.CreatedAtUtc))
            .ToList();
    }
}
