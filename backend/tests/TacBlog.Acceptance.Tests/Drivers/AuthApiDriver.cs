using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class AuthApiDriver(AuthContext authContext)
{
    private const string TestApiKey = "test-admin-api-key";

    public void Authenticate()
    {
        authContext.ApiKey = TestApiKey;
    }
}
