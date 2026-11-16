namespace Admiral.Playwright.Models.Geometry;

internal sealed record FileLabelBounds(
    [property: JsonPropertyName("nameRight")] double NameRight,
    [property: JsonPropertyName("sizeLeft")] double SizeLeft);
