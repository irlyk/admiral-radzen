namespace Admiral.Playwright.Models.Geometry;

internal sealed record ViewportDimensions(
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("height")] double Height);
