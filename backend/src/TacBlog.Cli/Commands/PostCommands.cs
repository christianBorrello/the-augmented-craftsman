using System.CommandLine;
using System.Text.RegularExpressions;

namespace TacBlog.Cli.Commands;

public static class PostCommands
{
    public static Command Build(Option<string> urlOpt, Option<string> keyOpt)
    {
        var postCmd = new Command("post", "Manage blog posts");

        postCmd.AddCommand(BuildNew(urlOpt, keyOpt));
        postCmd.AddCommand(BuildCreate(urlOpt, keyOpt));
        postCmd.AddCommand(BuildList(urlOpt, keyOpt));
        postCmd.AddCommand(BuildPublish(urlOpt, keyOpt));
        postCmd.AddCommand(BuildSchedule(urlOpt, keyOpt));
        postCmd.AddCommand(BuildArchive(urlOpt, keyOpt));
        postCmd.AddCommand(BuildRestore(urlOpt, keyOpt));
        postCmd.AddCommand(BuildDelete(urlOpt, keyOpt));

        return postCmd;
    }

    private static Command BuildCreate(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("create", "Create a new post");
        var titleOpt = new Option<string?>("--title", "Post title");
        var contentOpt = new Option<FileInfo?>("--content", "Path to markdown file (raw content, no frontmatter)");
        var fileOpt = new Option<FileInfo?>("--file", "Path to markdown file with YAML frontmatter");
        var draftOpt = new Option<bool>("--draft", "Save as draft without publishing");
        var tagsOpt = new Option<string[]>("--tag", "Tag name (repeatable)") { AllowMultipleArgumentsPerToken = false };

        draftOpt.SetDefaultValue(true);

        cmd.AddOption(titleOpt);
        cmd.AddOption(contentOpt);
        cmd.AddOption(fileOpt);
        cmd.AddOption(draftOpt);
        cmd.AddOption(tagsOpt);

        cmd.SetHandler(async (url, key, title, contentFile, file, draft, tags) =>
        {
            string content;
            string[] resolvedTags;
            string resolvedTitle;
            PostFrontmatter frontmatter = new();

            if (file is not null)
            {
                var raw = await File.ReadAllTextAsync(file.FullName);
                (frontmatter, var body) = FrontmatterParser.Parse(raw);

                resolvedTitle = title ?? frontmatter.Title;
                resolvedTags = (tags is { Length: > 0 } ? tags : frontmatter.Tags) ?? Array.Empty<string>();
                content = body;
            }
            else
            {
                resolvedTitle = title ?? string.Empty;
                resolvedTags = tags ?? Array.Empty<string>();
                content = contentFile is not null
                    ? await File.ReadAllTextAsync(contentFile.FullName)
                    : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(resolvedTitle))
            {
                Console.Error.WriteLine("Error: --title is required when --file is not provided.");
                return;
            }

            var client = new ApiClient(url, key);
            var result = await client.PostAsync("/api/posts", new
            {
                title = resolvedTitle,
                content,
                tags = resolvedTags
            });

            if (result is null) return;

            var id = result.Value.GetProperty("id").GetString()!;
            var slug = result.Value.GetProperty("slug").GetString();
            Console.WriteLine($"Created: {slug} ({id})");

            if (file is not null)
            {
                FrontmatterParser.WritePostId(file.FullName, id);

                if (frontmatter.ScheduledAt is null)
                    FrontmatterParser.WriteScheduledAt(file.FullName, FrontmatterParser.DefaultScheduledAt());
            }

            if (!draft)
            {
                await client.PostAsync($"/api/posts/{id}/publish");
                Console.WriteLine("Published.");

                if (file is not null)
                    MoveToPublished(file.FullName);
            }
        }, urlOpt, keyOpt, titleOpt, contentOpt, fileOpt, draftOpt, tagsOpt);

        return cmd;
    }

    private static Command BuildNew(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("new", "Create a new draft post and generate its .md file");
        var titleOpt = new Option<string>("--title", "Post title") { IsRequired = true };
        var tagsOpt = new Option<string[]>("--tag", "Tag name (repeatable)") { AllowMultipleArgumentsPerToken = false };
        var dirOpt = new Option<string>("--dir", "Directory to create the file in");

        dirOpt.SetDefaultValue("./docs/posts/drafts");

        cmd.AddOption(titleOpt);
        cmd.AddOption(tagsOpt);
        cmd.AddOption(dirOpt);

        cmd.SetHandler(async (url, key, title, tags, dir) =>
        {
            var scheduledAt = FrontmatterParser.DefaultScheduledAt();
            var resolvedTags = tags ?? Array.Empty<string>();

            var client = new ApiClient(url, key);
            var result = await client.PostAsync("/api/posts", new
            {
                title,
                content = "",
                tags = resolvedTags
            });

            if (result is null) return;

            var id = result.Value.GetProperty("id").GetString()!;
            var slug = result.Value.GetProperty("slug").GetString()!;

            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"{slug}.md");
            FrontmatterParser.CreateDraftFile(filePath, title, resolvedTags, id, scheduledAt);

            Console.WriteLine($"Created: {filePath}");
            Console.WriteLine($"Post ID: {id}");
        }, urlOpt, keyOpt, titleOpt, tagsOpt, dirOpt);

        return cmd;
    }

    private static void MoveToPublished(string filePath)
    {
        var readySegment = Path.DirectorySeparatorChar + "ready" + Path.DirectorySeparatorChar;
        if (!filePath.Contains(readySegment)) return;

        var publishedPath = filePath.Replace(readySegment, Path.DirectorySeparatorChar + "published" + Path.DirectorySeparatorChar);
        Directory.CreateDirectory(Path.GetDirectoryName(publishedPath)!);
        File.Move(filePath, publishedPath, overwrite: false);
        Console.WriteLine($"Moved to: {publishedPath}");
    }

    private static Command BuildSchedule(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("schedule", "Schedule a post for future publishing");
        var idArg = new Argument<string>("id", "Post ID (GUID)");
        var atOpt = new Option<string>("--at", "Publish datetime (ISO 8601 or 'tomorrow 9am')") { IsRequired = true };

        cmd.AddArgument(idArg);
        cmd.AddOption(atOpt);

        cmd.SetHandler(async (url, key, id, at) =>
        {
            var scheduledAt = ParseDatetime(at);
            if (scheduledAt is null)
            {
                Console.Error.WriteLine($"Error: could not parse datetime '{at}'. Use ISO 8601 (e.g. 2026-03-25T09:00:00Z) or 'tomorrow 9am'.");
                return;
            }

            var client = new ApiClient(url, key);
            var isoAt = scheduledAt.Value.ToString("o");
            var result = await client.PostAsync($"/api/posts/{id}/schedule?at={isoAt}");

            if (result is null) return;

            var slug = result.Value.GetProperty("slug").GetString();
            Console.WriteLine($"Scheduled: {slug} → {scheduledAt.Value:u}");
        }, urlOpt, keyOpt, idArg, atOpt);

        return cmd;
    }

    private static DateTime? ParseDatetime(string input)
    {
        // Try ISO 8601 first
        if (DateTime.TryParse(input, null, System.Globalization.DateTimeStyles.RoundtripKind, out var iso))
            return iso.ToUniversalTime();

        // Simple natural language: "tomorrow 9am", "tomorrow 14:00"
        var lower = input.Trim().ToLowerInvariant();
        var baseDate = lower.StartsWith("tomorrow") ? DateTime.UtcNow.Date.AddDays(1) : (DateTime?)null;

        if (baseDate is null) return null;

        var timeStr = lower.Replace("tomorrow", "").Trim();
        if (string.IsNullOrEmpty(timeStr)) return baseDate;

        var timeMatch = Regex.Match(timeStr, @"^(\d{1,2})(?::(\d{2}))?\s*(am|pm)?$");
        if (!timeMatch.Success) return null;

        var hour = int.Parse(timeMatch.Groups[1].Value);
        var minute = timeMatch.Groups[2].Success ? int.Parse(timeMatch.Groups[2].Value) : 0;
        var ampm = timeMatch.Groups[3].Value;

        if (ampm == "pm" && hour < 12) hour += 12;
        if (ampm == "am" && hour == 12) hour = 0;

        return baseDate.Value.AddHours(hour).AddMinutes(minute);
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
