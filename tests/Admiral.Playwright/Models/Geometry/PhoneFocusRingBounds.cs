namespace Admiral.Playwright.Models.Geometry;

internal sealed record PhoneFocusRingBounds(
    [property: JsonPropertyName("top")] double Top,
    [property: JsonPropertyName("bottom")] double Bottom,
    [property: JsonPropertyName("right")] double Right,
    [property: JsonPropertyName("fieldTop")] double FieldTop,
    [property: JsonPropertyName("fieldBottom")] double FieldBottom,
    [property: JsonPropertyName("textStart")] double TextStart);
