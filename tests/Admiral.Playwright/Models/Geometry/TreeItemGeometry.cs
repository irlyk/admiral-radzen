namespace Admiral.Playwright.Models.Geometry;

internal sealed record TreeItemGeometry(
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("icon")] double Icon,
    [property: JsonPropertyName("padding")] double Padding);
