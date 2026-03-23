using System.CommandLine;

namespace TacBlog.Cli.Commands;

public static class PostCommands
{
    public static Command Build(Option<string> urlOpt, Option<string> keyOpt)
    {
        var postCmd = new Command("post", "Manage blog posts");

        postCmd.AddCommand(BuildCreate(urlOpt, keyOpt));
        postCmd.AddCommand(BuildList(urlOpt, keyOpt));
        postCmd.AddCommand(BuildPublish(urlOpt, keyOpt));
        postCmd.AddCommand(BuildArchive(urlOpt, keyOpt));
        postCmd.AddCommand(BuildRestore(urlOpt, keyOpt));
        postCmd.AddCommand(BuildDelete(urlOpt, keyOpt));

        return postCmd;
    }

    private static Command BuildCreate(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("create", "Create a new post");
        var titleOpt = new Option<string>("--title", "Post title") { IsRequired = true };
        var contentOpt = new Option<FileInfo?>("--content", "Path to markdown file");
        var draftOpt = new Option<bool>("--draft", "Save as draft without publishing");
        var tagsOpt = new Option<string[]>("--tag", "Tag name (repeatable)") { AllowMultipleArgumentsPerToken = false };

        draftOpt.SetDefaultValue(true);

        cmd.AddOption(titleOpt);
        cmd.AddOption(contentOpt);
        cmd.AddOption(draftOpt);
        cmd.AddOption(tagsOpt);

        cmd.SetHandler(async (url, key, title, contentFile, draft, tags) =>
        {
            var content = contentFile is not null
                ? await File.ReadAllTextAsync(contentFile.FullName)
                : string.Empty;

            var client = new ApiClient(url, key);
            var result = await client.PostAsync("/api/posts", new
            {
                title,
                content,
                tags = tags ?? Array.Empty<string>()
            });

            if (result is null) return;

            var id = result.Value.GetProperty("id").GetString();
            var slug = result.Value.GetProperty("slug").GetString();
            Console.WriteLine($"Created: {slug} ({id})");

            if (!draft)
            {
                await client.PostAsync($"/api/posts/{id}/publish");
                Console.WriteLine("Published.");
            }
        }, urlOpt, keyOpt, titleOpt, contentOpt, draftOpt, tagsOpt);

        return cmd;
    }

    private static Command BuildList(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("list", "List posts");
        var statusOpt = new Option<string?>("--status", "Filter by status: draft, published, archived");
        cmd.AddOption(statusOpt);

        cmd.SetHandler(async (url, key, status) =>
        {
            var result = await new ApiClient(url, key).GetAsync("/api/admin/posts");
            if (result is null) return;

            foreach (var post in result.Value.EnumerateArray())
            {
                var postStatus = post.GetProperty("status").GetString();
                if (status is not null && !string.Equals(postStatus, status, StringComparison.OrdinalIgnoreCase))
                    continue;

                var title = post.GetProperty("title").GetString();
                var slug = post.GetProperty("slug").GetString();
                Console.WriteLine($"[{postStatus,-12}] {title} ({slug})");
            }
        }, urlOpt, keyOpt, statusOpt);

        return cmd;
    }

    private static Command BuildPublish(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("publish", "Publish a post by ID");
        var idArg = new Argument<string>("id", "Post ID (GUID)");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (url, key, id) =>
        {
            var result = await new ApiClient(url, key).PostAsync($"/api/posts/{id}/publish");
            if (result is not null) Console.WriteLine($"Published: {result.Value.GetProperty("slug").GetString()}");
        }, urlOpt, keyOpt, idArg);

        return cmd;
    }

    private static Command BuildArchive(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("archive", "Archive a post by ID");
        var idArg = new Argument<string>("id", "Post ID (GUID)");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (url, key, id) =>
        {
            var result = await new ApiClient(url, key).PostAsync($"/api/posts/{id}/archive");
            if (result is not null) Console.WriteLine($"Archived: {id}");
        }, urlOpt, keyOpt, idArg);

        return cmd;
    }

    private static Command BuildRestore(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("restore", "Restore an archived post by ID");
        var idArg = new Argument<string>("id", "Post ID (GUID)");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (url, key, id) =>
        {
            var result = await new ApiClient(url, key).PostAsync($"/api/posts/{id}/restore");
            if (result is not null) Console.WriteLine($"Restored: {id}");
        }, urlOpt, keyOpt, idArg);

        return cmd;
    }

    private static Command BuildDelete(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("delete", "Delete a post by ID");
        var idArg = new Argument<string>("id", "Post ID (GUID)");
        cmd.AddArgument(idArg);

        cmd.SetHandler(async (url, key, id) =>
        {
            if (await new ApiClient(url, key).DeleteAsync($"/api/posts/{id}"))
                Console.WriteLine($"Deleted: {id}");
        }, urlOpt, keyOpt, idArg);

        return cmd;
    }
}
