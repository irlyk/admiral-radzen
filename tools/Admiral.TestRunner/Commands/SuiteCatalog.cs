namespace Admiral.TestRunner.Commands;

internal static class SuiteCatalog
{
    private static readonly IReadOnlyDictionary<string, string[]> _categories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["audit"] = ["Audit"],
        ["checkbox"] = ["Checkbox"],
        ["interactions"] = ["Interactions"],
        ["combo"] = ["Combo"],
        ["tree-select"] = ["TreeSelect"],
        ["global-search"] = ["GlobalSearch"],
        ["drawer"] = ["Drawer"],
        ["hint"] = ["Hint"],
        ["slider"] = ["Slider"],
        ["phone-number"] = ["PhoneNumber"],
        ["select"] = ["Select"],
        ["visual"] = ["Visual"],
        ["file-input"] = ["FileInput"],
        ["image-viewer"] = ["ImageViewer"],
        ["action-bar"] = ["ActionBar"],
        ["tree"] = ["Tree"],
        ["native-date-range"] = ["NativeDateRange"],
        ["layout"] = ["Layout"],
        ["fonts"] = ["Fonts"],
        ["wasm"] = ["Wasm"],
        ["consumer"] = ["Consumer"],
        ["snippets"] = ["Snippets"],
        ["release"] = ["Release"],
        ["radzen-demos"] = ["Radzen"],
        ["docs"] = ["Documentation", "DocsUx", "DocsIteration"]
    };

    public static IEnumerable<string> Names => _categories.Keys.Order();
    public static string BrowserFilter => Excluding("Consumer", "Wasm", "Snippets", "Release", "Radzen");
    public static string AffectedFallbackFilter => Excluding("Radzen", "CleanRelease");

    public static bool RequiresOnlyPrivateHost(string name) => name is "consumer" or "wasm" or "release" or "radzen-demos";

    public static string Including(IEnumerable<string> categories) => string.Join(
        '|',
        categories.Distinct().Order().Select(category => "TestCategory=" + category));

    private static string Excluding(params string[] categories) =>
        string.Join('&', categories.Select(category => "TestCategory!=" + category));

    public static string? FilterFor(string name)
    {
        return name switch
        {
            "all" => null,
            "browser" => BrowserFilter,
            _ when _categories.TryGetValue(name, out var categories) => Including(categories),
            _ => throw new ArgumentException($"Unknown suite: {name}")
        };
    }
}
