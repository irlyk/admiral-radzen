namespace Admiral.Playwright.Models.Geometry;

internal sealed record ScrollViewportSnapshot(
    [property: JsonPropertyName("viewport")] double Viewport,
    [property: JsonPropertyName("content")] double Content,
    [property: JsonPropertyName("left")] double Left);
