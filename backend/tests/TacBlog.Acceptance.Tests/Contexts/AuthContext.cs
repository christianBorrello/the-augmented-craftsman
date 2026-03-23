namespace TacBlog.Acceptance.Tests.Contexts;

public sealed class AuthContext
{
    public string? ApiKey { get; set; }

    public bool IsAuthenticated => ApiKey is not null;
}
