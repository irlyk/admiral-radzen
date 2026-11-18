namespace Admiral.Playwright.Models.Geometry;

internal sealed record GroupedButtonGeometry(
    [property: JsonPropertyName("bounds")] DomRectangle Bounds,
    [property: JsonPropertyName("startStart")] double StartStart,
    [property: JsonPropertyName("startEnd")] double StartEnd,
    [property: JsonPropertyName("endStart")] double EndStart,
    [property: JsonPropertyName("endEnd")] double EndEnd,
    [property: JsonPropertyName("content")] DomRectangle Content);

internal sealed record ButtonGroupGeometry(
    [property: JsonPropertyName("group")] DomRectangle Group,
    [property: JsonPropertyName("buttons")] GroupedButtonGeometry[] Buttons);
