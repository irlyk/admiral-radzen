namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralActionItem(
    string Id,
    string Text,
    string Icon,
    bool Disabled = false,
    bool DividerBefore = false,
    string? AriaLabel = null);
