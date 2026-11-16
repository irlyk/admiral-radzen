namespace Admiral.Playwright.Models.Rendering;

internal sealed record ChartPaintStyle(
    [property: JsonPropertyName("fill")] string Fill,
    [property: JsonPropertyName("stroke")] string Stroke);
