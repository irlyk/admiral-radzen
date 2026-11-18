namespace Admiral.Playwright.Models.Geometry;

internal sealed record LayoutGeometry(
    [property: JsonPropertyName("root")] DomRectangle Root,
    [property: JsonPropertyName("header")] DomRectangle? Header,
    [property: JsonPropertyName("sidebar")] DomRectangle? Sidebar,
    [property: JsonPropertyName("body")] DomRectangle Body,
    [property: JsonPropertyName("footer")] DomRectangle? Footer,
    [property: JsonPropertyName("scrollHeight")] double ScrollHeight,
    [property: JsonPropertyName("clientHeight")] double ClientHeight);
