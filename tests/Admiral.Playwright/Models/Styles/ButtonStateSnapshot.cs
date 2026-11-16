namespace Admiral.Playwright.Models.Styles;

internal sealed record ButtonStateSnapshot(
    [property: JsonPropertyName("background")] string Background,
    [property: JsonPropertyName("disabled")] bool Disabled,
    [property: JsonPropertyName("ariaDisabled")] string AriaDisabled,
    [property: JsonPropertyName("busy")] string Busy);
