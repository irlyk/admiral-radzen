namespace Admiral.Playwright.Models.Geometry;

internal sealed record FocusClipping(
    [property: JsonPropertyName("zIndex")] double ZIndex,
    [property: JsonPropertyName("room")] FocusRingClearance? Room);
