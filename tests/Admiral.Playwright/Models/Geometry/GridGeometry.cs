namespace Admiral.Playwright.Models.Geometry;

internal sealed record GridGeometry(
    [property: JsonPropertyName("root")] DomRectangle Root,
    [property: JsonPropertyName("contentWidth")] double ContentWidth,
    [property: JsonPropertyName("gap")] double Gap,
    [property: JsonPropertyName("padding")] double Padding,
    [property: JsonPropertyName("columns")] int Columns,
    [property: JsonPropertyName("items")] DomRectangle[] Items);
