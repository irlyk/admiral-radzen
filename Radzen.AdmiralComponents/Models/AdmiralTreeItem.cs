namespace Radzen.Admiral.Components.Models;

/// <summary>A tree node. Id must be unique throughout the hierarchy.</summary>
public sealed record AdmiralTreeItem(string Id, string Text)
{
    public string? Icon { get; init; }
    public bool Disabled { get; init; }
    public IReadOnlyList<AdmiralTreeItem> Children { get; init; } = [];
}
