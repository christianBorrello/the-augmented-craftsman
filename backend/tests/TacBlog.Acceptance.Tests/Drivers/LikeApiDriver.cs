using System.Net.Http.Json;
using TacBlog.Acceptance.Tests.Contexts;

namespace TacBlog.Acceptance.Tests.Drivers;

public sealed class LikeApiDriver(HttpClient client, ApiContext apiContext)
{
    public async Task LikePost(string slug, string visitorId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/posts/{slug}/likes",
            new { visitorId });
        await apiContext.CaptureResponse(response);
    }

    public async Task GetLikeCount(string slug)
    {
        var response = await client.GetAsync($"/api/posts/{slug}/likes/count");
        await apiContext.CaptureResponse(response);
    }
}
