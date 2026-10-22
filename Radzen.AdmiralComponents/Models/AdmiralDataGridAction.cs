namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralDataGridAction<TItem>(string Id, string Icon)
{
    public string? Text { get; init; }
    public string? AriaLabel { get; init; }
    public bool DividerBefore { get; init; }
    public Func<TItem, bool>? IsDisabled { get; init; }
    public Func<TItem, string>? AriaLabelFor { get; init; }
    public Action<TItem>? Execute { get; init; }
    public Func<TItem, Task>? ExecuteAsync { get; init; }
}

public sealed record AdmiralDataGridActionEventArgs<TItem>(string ActionId, TItem Item);
