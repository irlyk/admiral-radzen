namespace Admiral.Playwright.Models.Styles;

internal sealed record FocusOutlineStyle(
    [property: JsonPropertyName("outline")] string Outline,
    [property: JsonPropertyName("outlineWidth")] double OutlineWidth,
    [property: JsonPropertyName("outlineOffset")] double OutlineOffset,
    [property: JsonPropertyName("clipMargin")] double ClipMargin);
