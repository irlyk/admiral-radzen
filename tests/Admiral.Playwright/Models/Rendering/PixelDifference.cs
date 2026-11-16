namespace Admiral.Playwright.Models.Rendering;

internal sealed record PixelDifference(
    [property: JsonPropertyName("changed")] double Changed,
    [property: JsonPropertyName("ratio")] double Ratio);
