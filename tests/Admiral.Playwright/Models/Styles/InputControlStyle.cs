namespace Admiral.Playwright.Models.Styles;

internal sealed record InputControlStyle(
    [property: JsonPropertyName("border")] string Border,
    [property: JsonPropertyName("shadow")] string Shadow,
    [property: JsonPropertyName("height")] double Height);
