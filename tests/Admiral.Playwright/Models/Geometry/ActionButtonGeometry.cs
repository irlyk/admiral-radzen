namespace Admiral.Playwright.Models.Geometry;

internal sealed record ActionButtonGeometry(
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("icon")] double Icon,
    [property: JsonPropertyName("dx")] double Dx,
    [property: JsonPropertyName("dy")] double Dy);
