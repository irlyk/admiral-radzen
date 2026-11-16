namespace Admiral.Playwright.Models.Geometry;

internal sealed record MenuItemGeometry(
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("icon")] double Icon);
