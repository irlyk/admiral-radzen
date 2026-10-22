namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralPaneColumn(
    string Id,
    string Text,
    bool Visible = true,
    bool Disabled = false);
