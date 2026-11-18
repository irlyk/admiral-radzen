namespace Admiral.Playwright.Models.Styles;

internal sealed record ButtonVisualState(
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("background")] string Background,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("border")] string Border,
    [property: JsonPropertyName("shadow")] string Shadow,
    [property: JsonPropertyName("opacity")] string Opacity);
