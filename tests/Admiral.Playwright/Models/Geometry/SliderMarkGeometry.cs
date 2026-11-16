namespace Admiral.Playwright.Models.Geometry;

internal sealed record SliderMarkGeometry(
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("lineHeight")] double LineHeight,
    [property: JsonPropertyName("whiteSpace")] string WhiteSpace);
