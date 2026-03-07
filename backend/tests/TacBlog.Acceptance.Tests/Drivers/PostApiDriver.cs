using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class PostApiDriver
{
    private readonly HttpClient _client;
    private readonly ApiContext _apiContext;
    private readonly AuthContext _authContext;

    public PostApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
    {
        _client = client;
        _apiContext = apiContext;
        _authContext = authContext;
    }

    public async Task CreatePost(string title, string content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/posts")
        {
            Content = JsonContent.Create(new { title, content })
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task CreatePostWithTags(string title, string content, string[] tags)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/posts")
        {
            Content = JsonContent.Create(new { title, content, tags })
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task GetPostBySlug(string slug)
    {
        var response = await _client.GetAsync($"/api/posts/{slug}");
        await _apiContext.CaptureResponse(response);
    }

    public async Task ListPosts()
    {
        var response = await _client.GetAsync("/api/posts");
        await _apiContext.CaptureResponse(response);
    }

    public async Task GetAdminPosts()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/posts");
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task PublishPost(string postId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/posts/{postId}/publish");
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task UpdatePost(string postId, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/posts/{postId}")
        {
            Content = JsonContent.Create(payload)
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task PreviewPost(string postId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/posts/{postId}/preview");
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task DeletePost(string postId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{postId}");
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
