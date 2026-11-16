namespace Admiral.Playwright.Models.Geometry;

internal sealed record PhoneInputGeometry(
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("countryRight")] double CountryRight,
    [property: JsonPropertyName("textStart")] double TextStart);
