using System.Net.Http.Json;
using System.Text.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class CommentApiDriver(HttpClient client, ApiContext apiContext, ReaderSessionContext sessionContext)
{
    public async Task PostComment(string slug, string text)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/posts/{slug}/comments")
        {
            Content = JsonContent.Create(new { text })
        };
        AddSessionCookieIfPresent(request);

        var response = await client.SendAsync(request);
        await apiContext.CaptureResponse(response);
    }

    public async Task PostCommentUnauthenticated(string slug, string text)
    {
        var response = await client.PostAsJsonAsync($"/api/posts/{slug}/comments", new { text });
        await apiContext.CaptureResponse(response);
    }

    public async Task GetComments(string slug)
    {
        var response = await client.GetAsync($"/api/posts/{slug}/comments");
        await apiContext.CaptureResponse(response);
    }

    public async Task GetCommentCount(string slug)
    {
        var response = await client.GetAsync($"/api/posts/{slug}/comments/count");
        await apiContext.CaptureResponse(response);
    }

    public async Task DeleteComment(string slug, Guid commentId, string? jwtToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{slug}/comments/{commentId}");
        if (jwtToken is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        var response = await client.SendAsync(request);
        await apiContext.CaptureResponse(response);
    }

    public async Task DeleteCommentWithReaderSession(string slug, Guid commentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{slug}/comments/{commentId}");
        AddSessionCookieIfPresent(request);

        var response = await client.SendAsync(request);
        await apiContext.CaptureResponse(response);
    }

    public async Task GetAdminComments(string? jwtToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/comments");
        if (jwtToken is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        var response = await client.SendAsync(request);
        await apiContext.CaptureResponse(response);
    }

    private void AddSessionCookieIfPresent(HttpRequestMessage request)
    {
        if (sessionContext.SessionCookie is not null)
            request.Headers.Add("Cookie", $"reader_session={sessionContext.SessionCookie}");
    }
}
