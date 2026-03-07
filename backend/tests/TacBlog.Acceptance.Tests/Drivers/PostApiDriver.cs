using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class PostApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
{
    public async Task CreatePost(string title, string content) =>
        await SendAuthenticatedAsync(HttpMethod.Post, "/api/posts",
            JsonContent.Create(new { title, content }));

    public async Task CreatePostWithTags(string title, string content, string[] tags) =>
        await SendAuthenticatedAsync(HttpMethod.Post, "/api/posts",
            JsonContent.Create(new { title, content, tags }));

    public async Task GetPostBySlug(string slug)
    {
        var response = await client.GetAsync($"/api/posts/{slug}");
        await apiContext.CaptureResponse(response);
    }

    public async Task ListPosts()
    {
        var response = await client.GetAsync("/api/posts");
        await apiContext.CaptureResponse(response);
    }

    public async Task GetAdminPosts() =>
        await SendAuthenticatedAsync(HttpMethod.Get, "/api/admin/posts");

    public async Task PublishPost(string postId) =>
        await SendAuthenticatedAsync(HttpMethod.Post, $"/api/posts/{postId}/publish");

    public async Task UpdatePost(string postId, object payload) =>
        await SendAuthenticatedAsync(HttpMethod.Put, $"/api/posts/{postId}",
            JsonContent.Create(payload));

    public async Task PreviewPost(string postId) =>
        await SendAuthenticatedAsync(HttpMethod.Get, $"/api/posts/{postId}/preview");

    public async Task DeletePost(string postId) =>
        await SendAuthenticatedAsync(HttpMethod.Delete, $"/api/posts/{postId}");

    private async Task SendAuthenticatedAsync(
        HttpMethod method,
        string path,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };

        if (authContext.IsAuthenticated)
            request.Headers.Authorization = new("Bearer", authContext.JwtToken);

        var response = await client.SendAsync(request);
        await apiContext.CaptureResponse(response);
    }
}
