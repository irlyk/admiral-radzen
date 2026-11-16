namespace Admiral.Playwright.Models.Geometry;

internal sealed record FocusRingClearance(
    [property: JsonPropertyName("left")] double Left,
    [property: JsonPropertyName("top")] double Top,
    [property: JsonPropertyName("right")] double Right);
