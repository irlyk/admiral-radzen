namespace Admiral.Playwright.Models.Styles;

internal sealed record ControlStateSnapshot(
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("background")] string Background,
    [property: JsonPropertyName("border")] string Border,
    [property: JsonPropertyName("radius")] string Radius,
    [property: JsonPropertyName("outline")] string Outline,
    [property: JsonPropertyName("offset")] string Offset,
    [property: JsonPropertyName("shadow")] string Shadow,
    [property: JsonPropertyName("fontWeight")] string FontWeight,
    [property: JsonPropertyName("focused")] bool Focused,
    [property: JsonPropertyName("focusVisible")] bool FocusVisible);
