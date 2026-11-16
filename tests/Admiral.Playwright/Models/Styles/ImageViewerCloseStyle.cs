namespace Admiral.Playwright.Models.Styles;

internal sealed record ImageViewerCloseStyle(
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("radius")] string Radius,
    [property: JsonPropertyName("outline")] string Outline,
    [property: JsonPropertyName("highlight")] string Highlight,
    [property: JsonPropertyName("icon")] double Icon);
