using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class AuthApiDriver
{
    private readonly HttpClient _client;
    private readonly ApiContext _apiContext;
    private readonly AuthContext _authContext;

    public AuthApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
    {
        _client = client;
        _apiContext = apiContext;
        _authContext = authContext;
    }

    public async Task Login(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        await _apiContext.CaptureResponse(response);

        if (response.IsSuccessStatusCode && _apiContext.LastResponseJson is not null)
        {
            _authContext.JwtToken = _apiContext.LastResponseJson
                .RootElement.GetProperty("token").GetString();
        }
    }

    public async Task Authenticate()
    {
        await Login("christian.borrello@live.it", "valid-password");
    }
}
