namespace Admiral.Playwright.Models.Geometry;

internal sealed record DomRectangle(
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("top")] double Top,
    [property: JsonPropertyName("left")] double Left,
    [property: JsonPropertyName("right")] double Right,
    [property: JsonPropertyName("bottom")] double Bottom);
