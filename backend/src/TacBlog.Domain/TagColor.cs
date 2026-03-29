namespace TacBlog.Domain;

public readonly record struct TagColor
{
    private static readonly IReadOnlyList<TagColor> PaletteColors =
    [
        new("#C2593A", "#E8845F"),
        new("#5B8C5A", "#7DB87C"),
        new("#B5566E", "#D87B93"),
        new("#4A6FA5", "#7B9FD4"),
        new("#C48B28", "#E0AD4E"),
        new("#7B5C8D", "#A683B8"),
        new("#3D8B8B", "#62B5B5"),
        new("#A06040", "#C8856A"),
    ];

    public static IReadOnlyList<TagColor> Palette => PaletteColors;

    private readonly string _light;
    private readonly string _dark;

    public TagColor(string light, string dark)
    {
        if (string.IsNullOrWhiteSpace(light))
            throw new ArgumentException("Light color is required", nameof(light));

        if (string.IsNullOrWhiteSpace(dark))
            throw new ArgumentException("Dark color is required", nameof(dark));

        _light = light;
        _dark = dark;
    }

    public string Light => _light;
    public string Dark => _dark;

    public static TagColor Random()
    {
        var index = System.Random.Shared.Next(PaletteColors.Count);
        return PaletteColors[index];
    }

    public static TagColor FromStoredValue(string light) =>
        PaletteColors.FirstOrDefault(c => c.Light == light) is { Light: not null } found
            ? found
            : throw new ArgumentException($"Unknown palette color: {light}", nameof(light));

    public override string ToString() => _light;
}
