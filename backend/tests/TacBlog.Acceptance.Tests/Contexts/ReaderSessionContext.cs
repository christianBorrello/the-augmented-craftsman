namespace TacBlog.Acceptance.Tests.Contexts;

public sealed class ReaderSessionContext
{
    public string? SessionCookie { get; set; }
    public bool IsAuthenticated => SessionCookie is not null;
}
