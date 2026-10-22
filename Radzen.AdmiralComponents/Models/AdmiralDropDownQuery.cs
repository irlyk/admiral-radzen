namespace Radzen.Admiral.Components.Models;

/// <summary>
/// Describes one filtered or virtualized Admiral dropdown request.
/// </summary>
public sealed record AdmiralDropDownQuery(string SearchText, int Skip, int? Take);
