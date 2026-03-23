using System.Net.Http.Json;
using System.Text.Json;

namespace TacBlog.Cli;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl, string apiKey)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        _http.DefaultRequestHeaders.Add("X-Admin-Key", apiKey);
    }

    public async Task<JsonElement?> GetAsync(string path)
    {
        var response = await _http.GetAsync(path);
        return await ReadJsonAsync(response);
    }

    public async Task<JsonElement?> PostAsync(string path, object? body = null)
    {
        var response = body is null
            ? await _http.PostAsync(path, null)
            : await _http.PostAsJsonAsync(path, body);
        return await ReadJsonAsync(response);
    }

    public async Task<JsonElement?> PutAsync(string path, object body)
    {
        var response = await _http.PutAsJsonAsync(path, body);
        return await ReadJsonAsync(response);
    }

    public async Task<bool> DeleteAsync(string path)
    {
        var response = await _http.DeleteAsync(path);
        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Error {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return false;
        }
        return true;
    }

    private static async Task<JsonElement?> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Error {(int)response.StatusCode}: {body}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(body)) return null;

        return JsonSerializer.Deserialize<JsonElement>(body);
    }
}
