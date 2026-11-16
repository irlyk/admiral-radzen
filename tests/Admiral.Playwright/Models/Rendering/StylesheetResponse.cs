namespace Admiral.Playwright.Models.Rendering;

internal sealed record StylesheetResponse(
    [property: JsonPropertyName("status")] double Status,
    [property: JsonPropertyName("length")] double Length);
