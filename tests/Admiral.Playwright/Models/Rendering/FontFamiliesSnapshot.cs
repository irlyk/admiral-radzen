namespace Admiral.Playwright.Models.Rendering;

internal sealed record FontFamiliesSnapshot(
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("heading")] string Heading,
    [property: JsonPropertyName("control")] string Control);
