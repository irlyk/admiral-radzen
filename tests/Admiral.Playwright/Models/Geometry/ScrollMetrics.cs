namespace Admiral.Playwright.Models.Geometry;

internal sealed record ScrollMetrics(
    [property: JsonPropertyName("scrollHeight")] double ScrollHeight,
    [property: JsonPropertyName("clientHeight")] double ClientHeight);
