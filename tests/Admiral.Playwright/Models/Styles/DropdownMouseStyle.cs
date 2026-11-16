namespace Admiral.Playwright.Models.Styles;

internal sealed record DropdownMouseStyle(
    [property: JsonPropertyName("visible")] bool Visible,
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("width")] string Width);
