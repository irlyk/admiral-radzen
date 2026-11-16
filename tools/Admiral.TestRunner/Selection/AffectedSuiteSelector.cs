using Admiral.TestRunner.Commands;

namespace Admiral.TestRunner.Selection;

internal sealed class AffectedSuiteSelector(RepositoryPaths paths)
{
    public AffectedSelection Select(IReadOnlyCollection<string> changedFiles)
    {
        if (changedFiles.Count == 0)
        {
            return AffectedSelection.Full("No changed files; baseline run");
        }

        var tests = new TestSourceCatalog(paths);
        var razor = new RazorDependencyGraph(paths.Root);
        var categories = new HashSet<string>();
        foreach (var file in changedFiles)
        {
            if (IsDocumentation(file))
            {
                continue;
            }

            if (file.StartsWith("tests/Admiral.Playwright/", StringComparison.Ordinal))
            {
                var suites = tests.CategoriesUsingFile(file);
                if (suites.Count == 0)
                {
                    return AffectedSelection.Full($"Unclassified test infrastructure: {file}");
                }

                categories.UnionWith(suites);
                continue;
            }

            if (file.StartsWith("samples/AdmiralConsumerValidation/", StringComparison.Ordinal))
            {
                categories.UnionWith(["Consumer", "Snippets"]);
                continue;
            }

            if (file.StartsWith("samples/AdmiralDocs/", StringComparison.Ordinal))
            {
                categories.Add("Wasm");
                continue;
            }

            var pages = razor.FindAffectedPages(file);
            if (pages is null || pages.Length == 0)
            {
                return AffectedSelection.Full($"Unclassified dependency: {file}");
            }

            categories.UnionWith(["Audit", "Layout", "Documentation"]);
            foreach (var page in pages)
            {
                var routes = razor.RoutesFor(page);
                var matching = tests.CategoriesForRoutes(routes);
                if (matching.Count == 0 && !routes.All(route => route is "/coverage" or "/icons"))
                {
                    return AffectedSelection.Full($"No browser suite maps to {page}");
                }

                categories.UnionWith(matching);
            }

            if (file.StartsWith("Radzen.Admiral", StringComparison.Ordinal))
            {
                categories.UnionWith(["Consumer", "Wasm"]);
            }
        }

        return new AffectedSelection(
            categories.Count == 0 ? null : SuiteCatalog.Including(categories),
            categories.Order().ToArray(),
            null);
    }

    private static bool IsDocumentation(string file) =>
        file.StartsWith("docs/", StringComparison.Ordinal)
        || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        || file is "package-lock.json" or ".nvmrc";
}

internal sealed record AffectedSelection(string? Filter, string[] Categories, string? FallbackReason)
{
    public static AffectedSelection Full(string reason) => new(SuiteCatalog.AffectedFallbackFilter, [], reason);
    public bool HasTests => Filter is not null;
}
