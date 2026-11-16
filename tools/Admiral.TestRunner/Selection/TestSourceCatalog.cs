using System.Text.RegularExpressions;

namespace Admiral.TestRunner.Selection;

internal sealed class TestSourceCatalog
{
    private static readonly Regex _typeNames = new(@"\b(?:class|record(?:\s+struct)?)\s+([A-Za-z0-9_]+)");
    private static readonly Regex _identifiers = new(@"\b[A-Z][A-Za-z0-9_]*\b");
    private static readonly Regex _category = new("\\bCategory(?:Attribute)?\\s*\\(\\s*\"([^\"]+)\"\\s*\\)");
    private static readonly Regex _route = new(
        @"/(?:components/[a-z-]+|regression/[a-z-]+|phone-regression|pagination-regression|tables)");
    private readonly Dictionary<string, SourceFile> _files;
    private readonly Dictionary<string, SourceFile> _types;
    private readonly SuiteSource[] _suites;

    public TestSourceCatalog(RepositoryPaths paths)
    {
        _files = Directory.EnumerateFiles(paths.TestDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .Select(
                file => new SourceFile(
                    Path.GetRelativePath(paths.Root, file).Replace('\\', '/'),
                    File.ReadAllText(file)))
            .ToDictionary(file => file.Path);
        _types = _files.Values.SelectMany(
            file => _typeNames.Matches(file.Text).Select(match => (Name: match.Groups[1].Value, File: file)))
            .GroupBy(type => type.Name)
            .ToDictionary(group => group.Key, group => group.First().File);
        _suites = _files.Values.Where(file => file.Path.EndsWith("Tests.cs", StringComparison.Ordinal))
            .Select(file => CreateSuite(file)).Where(suite => suite.Categories.Count > 0).ToArray();
    }

    public IReadOnlyCollection<string> CategoriesUsingFile(string path)
    {
        return _suites.Where(suite => suite.Dependencies.Contains(path))
            .SelectMany(suite => suite.Categories)
            .Distinct()
            .ToArray();
    }

    public IReadOnlyCollection<string> CategoriesForRoutes(IEnumerable<string> routes)
    {
        var affectedRoutes = routes.ToHashSet();
        return _suites.Where(suite => suite.Routes.Overlaps(affectedRoutes))
            .SelectMany(suite => suite.Categories)
            .Distinct()
            .ToArray();
    }

    private SuiteSource CreateSuite(SourceFile source)
    {
        var dependencies = new HashSet<string>();
        CollectDependencies(source, dependencies);
        var text = string.Join('\n', dependencies.Select(path => _files[path].Text));
        return new SuiteSource(
            _category.Matches(source.Text).Select(match => match.Groups[1].Value).ToHashSet(),
            _route.Matches(text).Select(match => match.Value).ToHashSet(),
            dependencies);
    }

    private void CollectDependencies(SourceFile source, HashSet<string> visited)
    {
        if (!visited.Add(source.Path))
        {
            return;
        }

        foreach (var identifier in _identifiers.Matches(source.Text).Select(match => match.Value).Distinct())
        {
            if (_types.TryGetValue(identifier, out var referenced))
            {
                CollectDependencies(referenced, visited);
            }
        }
    }

    private sealed record SourceFile(string Path, string Text);
    private sealed record SuiteSource(HashSet<string> Categories, HashSet<string> Routes, HashSet<string> Dependencies);
}
