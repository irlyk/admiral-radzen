namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralSearchItem(
    string Id,
    string Title,
    string? Meta = null,
    string? Icon = null);
