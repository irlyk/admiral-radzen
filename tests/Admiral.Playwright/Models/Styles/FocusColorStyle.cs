namespace Admiral.Playwright.Models.Styles;

internal sealed record FocusColorStyle(
    [property: JsonPropertyName("outline")] string Outline,
    [property: JsonPropertyName("bg")] string Bg);
