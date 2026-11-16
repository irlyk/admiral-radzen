namespace Admiral.Playwright.Documentation.Snippets;

internal sealed record SnippetCompilation(
    string Id,
    string Status,
    SnippetOccurrence[] Occurrences,
    string[] MissingSymbols,
    string[] Diagnostics,
    string[] Warnings,
    string Output)
{
    public bool Failed => Status is "compiler-warning" or "compiler-failure";
}
