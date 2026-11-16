namespace Admiral.Playwright.Documentation.Snippets;

internal sealed record RenderedExample(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("code")] string Code);
