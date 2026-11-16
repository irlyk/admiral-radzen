namespace Admiral.Playwright.Models.Styles;

internal sealed record FocusBackgroundStyle(
    [property: JsonPropertyName("outline")] string Outline,
    [property: JsonPropertyName("background")] string Background);
