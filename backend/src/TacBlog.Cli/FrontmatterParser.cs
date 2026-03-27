using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TacBlog.Cli;

public sealed class PostFrontmatter
{
    public string Title { get; init; } = "";
    public string[] Tags { get; init; } = [];
    public string? PostId { get; init; }
    public DateTime? ScheduledAt { get; init; }
}

public static class FrontmatterParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static (PostFrontmatter Frontmatter, string Body) Parse(string fileContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            fileContent,
            @"^---\r?\n([\s\S]*?)\r?\n---\r?\n?([\s\S]*)$"
        );

        if (!match.Success)
            return (new PostFrontmatter(), fileContent);

        var yaml = match.Groups[1].Value;
        var body = match.Groups[2].Value;

        var frontmatter = Deserializer.Deserialize<PostFrontmatter>(yaml)
            ?? new PostFrontmatter();

        return (frontmatter, body);
    }

    public static void WritePostId(string filePath, string postId)
        => WriteOrUpdateYamlField(filePath, "postId", postId);

    public static void WriteScheduledAt(string filePath, DateTime scheduledAt)
        => WriteOrUpdateYamlField(filePath, "scheduledAt", scheduledAt.ToString("yyyy-MM-ddTHH:mm:ssZ"));

    public static DateTime DefaultScheduledAt()
        => DateTime.Now.Date.AddDays(1).AddHours(9);

    public static void CreateDraftFile(string filePath, string title, string[] tags, string postId, DateTime scheduledAt)
    {
        var yaml = $"title: {title}\n" +
                   $"tags: [{string.Join(", ", tags)}]\n" +
                   $"postId: {postId}\n" +
                   $"scheduledAt: {scheduledAt:yyyy-MM-ddTHH:mm:ssZ}";

        File.WriteAllText(filePath, $"---\n{yaml}\n---\n\n");
    }

    private static void WriteOrUpdateYamlField(string filePath, string field, string value)
    {
        var raw = File.ReadAllText(filePath);
        var match = System.Text.RegularExpressions.Regex.Match(
            raw,
            @"^---\r?\n([\s\S]*?)\r?\n---\r?\n?([\s\S]*)$"
        );

        if (!match.Success) return;

        var yaml = match.Groups[1].Value;
        var body = match.Groups[2].Value;

        if (System.Text.RegularExpressions.Regex.IsMatch(yaml, $@"^{field}:.*$", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            yaml = System.Text.RegularExpressions.Regex.Replace(
                yaml,
                $@"^{field}:.*$",
                $"{field}: {value}",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
        }
        else
        {
            yaml = yaml.TrimEnd() + $"\n{field}: {value}";
        }

        File.WriteAllText(filePath, $"---\n{yaml}\n---\n{body}");
    }
}
