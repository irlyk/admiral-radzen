namespace Radzen.Admiral.Components.Models;

/// <summary>An item in a TreeSelect hierarchy. Id values must be unique throughout the tree.</summary>
public sealed class AdmiralTreeSelectItem
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<AdmiralTreeSelectItem> Children { get; init; } = [];
    public bool Disabled { get; init; }
    public bool ReadOnly { get; init; }
}
