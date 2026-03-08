using System.Net.Http.Json;
using System.Text.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class OAuthApiDriver(HttpClient client, ApiContext apiContext, ReaderSessionContext sessionContext)
{
    public async Task SimulateCallback(string provider, string code = "test-code", string state = "/")
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/auth/oauth/{provider}/callback?code={code}&state={state}");
        AddSessionCookieIfPresent(request);

        var response = await client.SendAsync(request);
        apiContext.LastResponse = response;
        apiContext.LastResponseJson = null;

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.StartsWith("reader_session=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = cookie.Split('=', 2)[1].Split(';')[0];
                    sessionContext.SessionCookie = value;
                }
            }
        }
    }

    public async Task CheckSession()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/session");
        AddSessionCookieIfPresent(request);

        var response = await client.SendAsync(request);
        apiContext.LastResponse = response;

        var content = await response.Content.ReadAsStringAsync();
        apiContext.LastResponseJson = JsonDocument.Parse(content);
    }

    public async Task SignOut()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/signout");
        AddSessionCookieIfPresent(request);

        var response = await client.SendAsync(request);
        apiContext.LastResponse = response;
        apiContext.LastResponseJson = null;

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.Contains("reader_session=;") || cookie.Contains("expires=Thu, 01 Jan 1970"))
                {
                    sessionContext.SessionCookie = null;
                }
            }
        }
    }

    public async Task InitiateOAuth(string provider, string returnUrl = "/")
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/auth/oauth/{provider}?returnUrl={Uri.EscapeDataString(returnUrl)}");

        var response = await client.SendAsync(request);
        apiContext.LastResponse = response;
        apiContext.LastResponseJson = null;
    }

    private void AddSessionCookieIfPresent(HttpRequestMessage request)
    {
        if (sessionContext.SessionCookie is not null)
            request.Headers.Add("Cookie", $"reader_session={sessionContext.SessionCookie}");
    }
}
