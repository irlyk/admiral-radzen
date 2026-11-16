namespace Admiral.Playwright.Models.Geometry;

internal sealed record IconOffsetSnapshot(
    [property: JsonPropertyName("dx")] double Dx,
    [property: JsonPropertyName("dy")] double Dy);
