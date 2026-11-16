namespace Admiral.Playwright.Models.Geometry;

internal sealed record PopupGeometry(
    [property: JsonPropertyName("panel")] DomRectangle Panel,
    [property: JsonPropertyName("list")] DomRectangle List,
    [property: JsonPropertyName("viewport")] ViewportDimensions Viewport,
    [property: JsonPropertyName("scrollHeight")] double ScrollHeight);
