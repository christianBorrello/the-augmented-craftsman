namespace TacBlog.Domain;

public sealed class ReaderSession
{
    public Guid Id { get; }
    public string DisplayName { get; }
    public string? AvatarUrl { get; }
    public AuthProvider Provider { get; }
    public string ProviderId { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }

    private ReaderSession(
        Guid id,
        string displayName,
        string? avatarUrl,
        AuthProvider provider,
        string providerId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        Id = id;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Provider = provider;
        ProviderId = providerId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static ReaderSession Create(
        string displayName,
        string? avatarUrl,
        AuthProvider provider,
        string providerId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc) =>
        new(Guid.NewGuid(), displayName, avatarUrl, provider, providerId, createdAtUtc, expiresAtUtc);

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}
