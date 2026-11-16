namespace Admiral.Playwright.Documentation.Snippets;

internal sealed record SnippetInventory(
    DocumentationSource[] Pages,
    string[] ScanErrors,
    SnippetOccurrence[] Occurrences,
    RenderedSnippet[] Snippets);
