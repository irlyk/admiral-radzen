namespace Admiral.Playwright.Models.Geometry;

internal sealed record GroupActionGeometry(
    [property: JsonPropertyName("visible")] bool Visible,
    [property: JsonPropertyName("offset")] double Offset,
    [property: JsonPropertyName("gap")] double Gap,
    [property: JsonPropertyName("height")] double Height,
    [property: JsonPropertyName("paneHeight")] double PaneHeight);
