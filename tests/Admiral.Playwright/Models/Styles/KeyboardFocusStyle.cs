namespace Admiral.Playwright.Models.Styles;

internal sealed record KeyboardFocusStyle(
    [property: JsonPropertyName("visible")] bool Visible,
    [property: JsonPropertyName("border")] string Border,
    [property: JsonPropertyName("width")] string Width);
