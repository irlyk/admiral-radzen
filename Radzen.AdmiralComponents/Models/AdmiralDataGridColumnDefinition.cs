using Microsoft.AspNetCore.Components;
using Radzen;

namespace Radzen.Admiral.Components.Models;

public sealed record AdmiralDataGridColumnDefinition<TItem>(string Id, string Property, string Title)
{
    public string? Width { get; init; }
    public string? MinWidth { get; init; }
    public string? FormatString { get; init; }
    public TextAlign TextAlign { get; init; } = TextAlign.Left;
    public bool Visible { get; init; } = true;
    public bool DisabledInPicker { get; init; }
    public bool Sortable { get; init; } = true;
    public bool Filterable { get; init; } = true;
    public bool Resizable { get; init; } = true;
    public bool Reorderable { get; init; } = true;
    public bool Searchable { get; init; } = true;
    public RenderFragment<TItem>? Template { get; init; }
    public RenderFragment? HeaderTemplate { get; init; }
}
