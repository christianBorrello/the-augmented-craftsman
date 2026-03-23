using System.CommandLine;
using System.Net.Http.Headers;

namespace TacBlog.Cli.Commands;

public static class ImageCommands
{
    public static Command Build(Option<string> urlOpt, Option<string> keyOpt)
    {
        var imageCmd = new Command("image", "Manage images");
        imageCmd.AddCommand(BuildUpload(urlOpt, keyOpt));
        return imageCmd;
    }

    private static Command BuildUpload(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("upload", "Upload an image file");
        var fileArg = new Argument<FileInfo>("file", "Image file path");
        var postOpt = new Option<string?>("--post", "Post slug to set as featured image");
        cmd.AddArgument(fileArg);
        cmd.AddOption(postOpt);

        cmd.SetHandler(async (url, key, file, postSlug) =>
        {
            if (!file.Exists)
            {
                Console.Error.WriteLine($"File not found: {file.FullName}");
                return;
            }

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url.TrimEnd('/') + "/");
            httpClient.DefaultRequestHeaders.Add("X-Admin-Key", key);

            using var form = new MultipartFormDataContent();
            var bytes = await File.ReadAllBytesAsync(file.FullName);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(file.Extension));
            form.Add(fileContent, "file", file.Name);

            var response = await httpClient.PostAsync("api/images", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Upload failed {(int)response.StatusCode}: {body}");
                return;
            }

            Console.WriteLine($"Uploaded: {body}");

            if (postSlug is not null)
            {
                var imageUrl = body.Trim('"');
                var setResult = await new ApiClient(url, key)
                    .PutAsync($"/api/posts/{postSlug}/featured-image", new { imageUrl });
                if (setResult is not null) Console.WriteLine($"Set as featured image for: {postSlug}");
            }
        }, urlOpt, keyOpt, fileArg, postOpt);

        return cmd;
    }

    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/png"
    };
}
