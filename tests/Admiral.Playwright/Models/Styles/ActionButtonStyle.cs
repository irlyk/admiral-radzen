namespace Admiral.Playwright.Models.Styles;

internal sealed record ActionButtonStyle(
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("background")] string Background,
    [property: JsonPropertyName("outline")] string Outline,
    [property: JsonPropertyName("radius")] string Radius);
