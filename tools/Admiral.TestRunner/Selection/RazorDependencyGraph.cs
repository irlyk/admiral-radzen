using System.Text.RegularExpressions;

namespace Admiral.TestRunner.Selection;

internal sealed class RazorDependencyGraph
{
    private static readonly Regex _componentTag = new(@"<\s*(Admiral[A-Za-z0-9]+|Radzen[A-Za-z0-9]+)");
    private static readonly Regex _pageRoute = new("@page\\s+\"([^\"]+)\"");
    private static readonly Regex _cssSelector = new(@"\.((?:admiral|rz)-[a-z0-9_-]+)");
    private static readonly HashSet<string> _sharedSelectors = ["rz-dropdown", "rz-dropdown-panel", "rz-dropdown-item", "rz-inputtext"];
    private readonly string _root;
    private readonly Dictionary<string, string> _components;
    private readonly Dictionary<string, string> _pages;

    public RazorDependencyGraph(string root)
    {
        _root = root;
        _components = Directory.GetFiles(Path.Combine(root, "Radzen.AdmiralComponents"), "*.razor")
            .ToDictionary(file => Path.GetFileNameWithoutExtension(file)!, File.ReadAllText);
        _pages = Directory.GetFiles(Path.Combine(root, "samples/AdmiralConsumerSmoke/Pages"), "*.razor")
            .ToDictionary(file => Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText);
    }

    public string[]? FindAffectedPages(string file)
    {
        var component = Path.GetFileName(file).Replace(".razor.cs", "").Replace(".razor", "");
        if (file.StartsWith("Radzen.AdmiralComponents/", StringComparison.Ordinal)
            && _components.ContainsKey(component))
        {
            return _pages.Where(page => ComponentClosure(page.Value).Contains(component))
                .Select(page => page.Key)
                .ToArray();
        }

        if (_pages.ContainsKey(file))
        {
            return [file];
        }

        return file.StartsWith("Radzen.AdmiralThemes/themes/", StringComparison.Ordinal)
            && file.EndsWith(".scss", StringComparison.Ordinal)
            ? PagesMatchingSelectors(file)
            : null;
    }

    public string[] RoutesFor(string page) =>
        _pageRoute.Matches(_pages[page]).Select(match => match.Groups[1].Value).ToArray();

    private string[]? PagesMatchingSelectors(string file)
    {
        var selectors = _cssSelector.Matches(File.ReadAllText(Path.Combine(_root, file)))
            .Select(match => match.Groups[1].Value)
            .ToHashSet();
        if (selectors.Count == 0 || selectors.Overlaps(_sharedSelectors))
        {
            return null;
        }

        return _pages.Where(
            page => new[] { page.Value }
                .Concat(
                    ComponentClosure(page.Value)
                    .Where(_components.ContainsKey)
                    .Select(component => _components[component]))
                .Any(source => selectors.Any(selector => source.Contains(selector, StringComparison.Ordinal))))
            .Select(page => page.Key)
            .ToArray();
    }

    private HashSet<string> ComponentClosure(string markup, HashSet<string>? visited = null)
    {
        visited ??= [];
        foreach (var name in _componentTag.Matches(markup).Select(match => match.Groups[1].Value))
        {
            if (visited.Add(name) && _components.TryGetValue(name, out var source))
            {
                ComponentClosure(source, visited);
            }
        }

        return visited;
    }
}
