namespace Admiral.Playwright.Models.Geometry;

internal sealed record TreeSelectChipGeometry(
    [property: JsonPropertyName("scrollHeight")] double ScrollHeight,
    [property: JsonPropertyName("clientHeight")] double ClientHeight,
    [property: JsonPropertyName("scrollWidth")] double ScrollWidth,
    [property: JsonPropertyName("clientWidth")] double ClientWidth,
    [property: JsonPropertyName("buttonWidth")] double ButtonWidth,
    [property: JsonPropertyName("buttonHeight")] double ButtonHeight,
    [property: JsonPropertyName("iconWidth")] double IconWidth,
    [property: JsonPropertyName("iconHeight")] double IconHeight,
    [property: JsonPropertyName("dx")] double Dx,
    [property: JsonPropertyName("dy")] double Dy,
    [property: JsonPropertyName("fieldDy")] double FieldDy);
