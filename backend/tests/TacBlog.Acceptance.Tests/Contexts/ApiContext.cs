using System.Net;
using System.Text.Json;

namespace TacBlog.Acceptance.Tests.Contexts;

public sealed class ApiContext
{
    public HttpResponseMessage? LastResponse { get; set; }
    public string? LastResponseBody { get; set; }
    public JsonDocument? LastResponseJson { get; set; }

    public HttpStatusCode StatusCode =>
        LastResponse?.StatusCode ?? throw new InvalidOperationException("No response recorded.");

    public async Task CaptureResponse(HttpResponseMessage response)
    {
        LastResponse = response;
        LastResponseBody = await response.Content.ReadAsStringAsync();

        if (response.Content.Headers.ContentType?.MediaType == "application/json"
            && !string.IsNullOrWhiteSpace(LastResponseBody))
        {
            LastResponseJson = JsonDocument.Parse(LastResponseBody);
        }
    }
}
