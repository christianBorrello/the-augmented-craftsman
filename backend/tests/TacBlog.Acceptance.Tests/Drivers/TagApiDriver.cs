using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class TagApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
{
    public async Task CreateTag(string name) =>
        await SendAuthenticatedAsync(HttpMethod.Post, "/api/tags",
            JsonContent.Create(new { name }));

    public async Task ListTags()
    {
        var response = await client.GetAsync("/api/tags");
        await apiContext.CaptureResponse(response);
    }

    public async Task RenameTag(string slug, string newName) =>
        await SendAuthenticatedAsync(HttpMethod.Put, $"/api/tags/{slug}",
            JsonContent.Create(new { name = newName }));

    public async Task DeleteTag(string slug) =>
        await SendAuthenticatedAsync(HttpMethod.Delete, $"/api/tags/{slug}");

    public async Task ListPublicTags()
    {
        var response = await client.GetAsync("/api/tags");
        await apiContext.CaptureResponse(response);
    }

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
