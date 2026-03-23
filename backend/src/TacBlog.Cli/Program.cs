using System.CommandLine;
using TacBlog.Cli;
using TacBlog.Cli.Commands;

var urlOpt = new Option<string>(
    "--url",
    () => Environment.GetEnvironmentVariable("TAC_API_URL") ?? "http://localhost:5205",
    "API base URL");

var keyOpt = new Option<string>(
    "--key",
    () => Environment.GetEnvironmentVariable("TAC_API_KEY") ?? "dev-admin-key",
    "Admin API key");

var root = new RootCommand("tac — The Augmented Craftsman CLI");
root.AddGlobalOption(urlOpt);
root.AddGlobalOption(keyOpt);

root.AddCommand(PostCommands.Build(urlOpt, keyOpt));
root.AddCommand(TagCommands.Build(urlOpt, keyOpt));
root.AddCommand(ImageCommands.Build(urlOpt, keyOpt));

return await root.InvokeAsync(args);
