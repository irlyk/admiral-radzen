namespace Admiral.Playwright.Models.Rendering;

internal sealed record ChartSeriesSnapshot(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("painted")] ChartPaintStyle[] Painted);
