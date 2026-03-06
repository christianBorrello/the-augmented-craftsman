using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class TagApiDriver
{
    private readonly HttpClient _client;
    private readonly ApiContext _apiContext;
    private readonly AuthContext _authContext;

    public TagApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
    {
        _client = client;
        _apiContext = apiContext;
        _authContext = authContext;
    }

    public async Task CreateTag(string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tags")
        {
            Content = JsonContent.Create(new { name })
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task ListTags()
    {
        var response = await _client.GetAsync("/api/tags");
        await _apiContext.CaptureResponse(response);
    }

    public async Task RenameTag(string tagId, string newName)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tags/{tagId}")
        {
            Content = JsonContent.Create(new { name = newName })
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task DeleteTag(string tagId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/tags/{tagId}");
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (_authContext.IsAuthenticated)
            request.Headers.Authorization = new("Bearer", _authContext.JwtToken);
    }
}
