namespace Admiral.Playwright.Models.Geometry;

internal sealed record SliderLayoutSnapshot(
    [property: JsonPropertyName("slider")] DomRectangle Slider,
    [property: JsonPropertyName("preview")] DomRectangle Preview,
    [property: JsonPropertyName("controls")] DomRectangle Controls);
