namespace TacBlog.Acceptance.Tests.Contexts;

public sealed class AuthContext
{
    public string? JwtToken { get; set; }

    public bool IsAuthenticated => JwtToken is not null;
}
