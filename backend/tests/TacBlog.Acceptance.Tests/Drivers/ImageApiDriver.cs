using System.Net.Http.Headers;
using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class ImageApiDriver(HttpClient client, ApiContext apiContext, AuthContext authContext)
{
    public Task UploadImage(string fileName, byte[] content) =>
        SendMultipartUploadAsync(fileName, content, "image/png");

    public Task UploadNonImageFile(string fileName, byte[] content) =>
        SendMultipartUploadAsync(fileName, content, "application/pdf");

    public Task SetFeaturedImage(string slug, string imageUrl) =>
        SendAuthenticatedAsync(
            new HttpRequestMessage(HttpMethod.Put, $"/api/posts/{slug}/featured-image")
            {
                Content = JsonContent.Create(new { imageUrl })
            });

    public Task RemoveFeaturedImage(string slug) =>
        SendAuthenticatedAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{slug}/featured-image"));

    private async Task SendMultipartUploadAsync(string fileName, byte[] content, string contentType)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        await SendAuthenticatedAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/images") { Content = form });
    }

    private async Task SendAuthenticatedAsync(HttpRequestMessage request)
    {
        if (authContext.IsAuthenticated)
            request.Headers.Authorization = new("Bearer", authContext.JwtToken);

        var response = await client.SendAsync(request);
        await apiContext.CaptureResponse(response);
    }
}
