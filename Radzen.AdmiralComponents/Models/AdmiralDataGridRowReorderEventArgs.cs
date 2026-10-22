namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralDataGridRowReorderEventArgs<TItem>(
    TItem Item,
    int OldIndex,
    int NewIndex,
    string RowKey,
    string? NextRowKey);
