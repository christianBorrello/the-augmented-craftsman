using System.CommandLine;

namespace TacBlog.Cli.Commands;

public static class TagCommands
{
    public static Command Build(Option<string> urlOpt, Option<string> keyOpt)
    {
        var tagCmd = new Command("tag", "Manage tags");

        tagCmd.AddCommand(BuildCreate(urlOpt, keyOpt));
        tagCmd.AddCommand(BuildList(urlOpt, keyOpt));
        tagCmd.AddCommand(BuildDelete(urlOpt, keyOpt));

        return tagCmd;
    }

    private static Command BuildCreate(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("create", "Create a new tag");
        var nameArg = new Argument<string>("name", "Tag name");
        cmd.AddArgument(nameArg);

        cmd.SetHandler(async (url, key, name) =>
        {
            var result = await new ApiClient(url, key).PostAsync("/api/tags", new { name });
            if (result is not null)
                Console.WriteLine($"Created: {result.Value.GetProperty("name").GetString()} ({result.Value.GetProperty("slug").GetString()})");
        }, urlOpt, keyOpt, nameArg);

        return cmd;
    }

    private static Command BuildList(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("list", "List all tags");

        cmd.SetHandler(async (url, key) =>
        {
            var result = await new ApiClient(url, key).GetAsync("/api/admin/tags");
            if (result is null) return;

            foreach (var tag in result.Value.EnumerateArray())
            {
                var name = tag.GetProperty("name").GetString();
                var slug = tag.GetProperty("slug").GetString();
                Console.WriteLine($"{name} ({slug})");
            }
        }, urlOpt, keyOpt);

        return cmd;
    }

    private static Command BuildDelete(Option<string> urlOpt, Option<string> keyOpt)
    {
        var cmd = new Command("delete", "Delete a tag by slug");
        var slugArg = new Argument<string>("slug", "Tag slug");
        cmd.AddArgument(slugArg);

        cmd.SetHandler(async (url, key, slug) =>
        {
            if (await new ApiClient(url, key).DeleteAsync($"/api/tags/{slug}"))
                Console.WriteLine($"Deleted: {slug}");
        }, urlOpt, keyOpt, slugArg);

        return cmd;
    }
}
