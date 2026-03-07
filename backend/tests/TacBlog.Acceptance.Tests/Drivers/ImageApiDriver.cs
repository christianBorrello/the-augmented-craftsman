using System.Net.Http.Headers;
using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class ImageApiDriver
{
    private readonly HttpClient _client;
    private readonly ApiContext _apiContext;
    private readonly AuthContext _authContext;

    public ImageApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
    {
        _client = client;
        _apiContext = apiContext;
        _authContext = authContext;
    }

    public async Task UploadImage(string fileName, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/images")
        {
            Content = form
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task UploadNonImageFile(string fileName, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/images")
        {
            Content = form
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task SetFeaturedImage(string slug, string imageUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/posts/{slug}/featured-image")
        {
            Content = JsonContent.Create(new { imageUrl })
        };
        ApplyAuth(request);

        var response = await _client.SendAsync(request);
        await _apiContext.CaptureResponse(response);
    }

    public async Task RemoveFeaturedImage(string slug)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{slug}/featured-image");
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
