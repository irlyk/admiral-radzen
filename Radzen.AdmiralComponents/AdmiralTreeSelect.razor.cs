using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Models;
using static Radzen.Admiral.Components.Internal.AdmiralTreeSelectItems;

namespace Radzen.Admiral.Components;

public partial class AdmiralTreeSelect : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public IReadOnlyList<AdmiralTreeSelectItem> Items { get; set; } = [];

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public IReadOnlyCollection<string> Values { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ValuesChanged { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// Display strategy for multiple-selection chips. Defaults to Parent for compatibility.
    /// Does not change Values, cascade selection or ValuesChanged payloads.
    /// </summary>
    [Parameter]
    public AdmiralTreeSelectCheckedStrategy ShowCheckedStrategy { get; set; } = AdmiralTreeSelectCheckedStrategy.Parent;

    [Parameter]
    public bool Searchable { get; set; } = true;

    [Parameter]
    public bool AllowClear { get; set; } = true;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Shows a spinner and prevents opening or changing the selection.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Replaces the field with an inert, theme-aware skeleton.</summary>
    [Parameter]
    public bool Skeleton { get; set; }

    /// <summary>Minimum chip rows in multiple mode. Null means one natural row.</summary>
    [Parameter]
    public int? MinRowCount { get; set; }

    /// <summary>Maximum visible chip rows while closed. Null means unlimited. Open fields show all rows.</summary>
    [Parameter]
    public int? MaxRowCount { get; set; }

    /// <summary>Optional content above the search and tree, outside the tree's scroll area.</summary>
    [Parameter]
    public RenderFragment? TopPanelTemplate { get; set; }

    /// <summary>Optional content below the tree, outside the tree's scroll area.</summary>
    [Parameter]
    public RenderFragment? BottomPanelTemplate { get; set; }

    /// <summary>Localized accessible label for the loading spinner.</summary>
    [Parameter]
    public string LoadingAriaLabel { get; set; } = "Загрузка";

    /// <summary>Localized prefix for the accessible name of each chip removal button.</summary>
    [Parameter]
    public string RemoveChipAriaLabel { get; set; } = "Удалить";

    [Parameter]
    public AdmiralInputSize Size { get; set; } = AdmiralInputSize.Medium;

    [Parameter]
    public string Placeholder { get; set; } = "Выберите значение";

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string SearchPlaceholder { get; set; } = "Поиск";

    [Parameter]
    public string EmptyText { get; set; } = "Ничего не найдено";

    [Parameter]
    public string ClearAriaLabel { get; set; } = "Очистить";

    [Parameter]
    public string CloseAriaLabel { get; set; } = "Закрыть список";

    [Parameter]
    public string ExpandAriaLabel { get; set; } = "Раскрыть";

    [Parameter]
    public string CollapseAriaLabel { get; set; } = "Свернуть";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private bool isOpen;
    private readonly string instanceId = $"admiral-tree-select-{Guid.NewGuid():N}";
    private string TreeId => $"{instanceId}-tree";
    private string? activeId;
    private string? ActiveDescendant => isOpen && activeId is not null ? $"{TreeId}-{Uri.EscapeDataString(activeId)}" : null;
    private ElementReference root;
    private ElementReference trigger;
    private Internal.AdmiralComboBoxInterop<AdmiralTreeSelect>? interop;
    private string query = "";
    private readonly HashSet<string> expandedIds = [];
    private IReadOnlySet<string> EffectiveSelected => Multiple
        ? new HashSet<string>(Values)
        : Value is null ? new HashSet<string>() : new HashSet<string> { Value };
    private string CssClass => $"admiral-tree-select admiral-tree-select--{(Size == AdmiralInputSize.Small ? "s" : Size == AdmiralInputSize.ExtraLarge ? "xl" : "m")} {(Disabled ? "admiral-tree-select--disabled" : null)} {(ReadOnly ? "admiral-tree-select--readonly" : null)} {Class}";
    private string? SingleLabel => Find(Items, Value)?.Text;
    private bool HasSelection => Multiple ? Values.Count > 0 : Value is not null;
    private IReadOnlyList<AdmiralTreeSelectItem> SelectedItems { get; set; } = [];
    private bool IsInteractionBlocked => Disabled || ReadOnly || Loading || Skeleton;
    private string RowsStyle => $"--admiral-tree-select-min-rows:{MinRowCount ?? 1};--admiral-tree-select-max-rows:{MaxRowCount ?? 1};";

    protected override void OnParametersSet()
    {
        if (MinRowCount is < 1 || MaxRowCount is < 1 || (MinRowCount.HasValue && MaxRowCount.HasValue && MinRowCount > MaxRowCount))
        {
            throw new ArgumentOutOfRangeException(nameof(MinRowCount), "Row limits must be positive, and MinRowCount must not exceed MaxRowCount.");
        }
        SelectedItems = SelectedNodes(Items, new HashSet<string>(Values), ShowCheckedStrategy).ToArray();
        if (IsInteractionBlocked)
        {
            Close();
        }
        if (activeId is not null && !VisibleItems(Items).Any(item => item.Id == activeId))
        {
            activeId = null;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        interop ??= new(JS, this, "tree");
        await interop.ConnectAsync(root);
    }

    private void ToggleOpen()
    {
        if (IsInteractionBlocked)
        {
            return;
        }

        if (isOpen)
        {
            Close();
        }
        else
        {
            isOpen = true;
        }
    }

    private void Close()
    {
        isOpen = false;
        query = string.Empty;
        activeId = null;
    }

    [JSInvokable]
    public Task DismissPopup() => InvokeAsync(() =>
    {
        Close();
        StateHasChanged();
    });

    private void Search(ChangeEventArgs args)
    {
        query = args.Value?.ToString() ?? string.Empty;
        activeId = null;
    }

    [JSInvokable]
    public async Task HandlePopupKey(string key, bool fromSearch)
    {
        if (IsInteractionBlocked)
        {
            return;
        }
        if (key == "Escape")
        {
            Close();
            await trigger.FocusAsync(preventScroll: true);
        }
        else if (!isOpen)
        {
            if (key is not ("Enter" or " " or "ArrowDown" or "ArrowUp"))
            {
                return;
            }
            isOpen = true;
            activeId = key == "ArrowUp" ? VisibleItems(Items).LastOrDefault()?.Id : VisibleItems(Items).FirstOrDefault()?.Id;
        }
        else
        {
            var visible = VisibleItems(Items).ToList();
            var index = visible.FindIndex(item => item.Id == activeId);
            var active = index >= 0 ? visible[index] : null;
            switch (key)
            {
                case "ArrowDown":
                    activeId = visible.ElementAtOrDefault(Math.Min(index + 1, visible.Count - 1))?.Id;
                    break;
                case "ArrowUp":
                    activeId = visible.ElementAtOrDefault(index < 0 ? visible.Count - 1 : Math.Max(index - 1, 0))?.Id;
                    break;
                case "Home" when !fromSearch:
                    activeId = visible.FirstOrDefault()?.Id;
                    break;
                case "End" when !fromSearch:
                    activeId = visible.LastOrDefault()?.Id;
                    break;
                case "ArrowRight" when !fromSearch && active?.Children.Count > 0:
                    ExpandOrMoveToChild(active);
                    break;
                case "ArrowLeft" when !fromSearch && active is not null:
                    CollapseOrMoveToParent(active);
                    break;
                case "Enter" or " " when active is not null:
                    await SelectAsync(active);
                    break;
            }
        }
        await InvokeAsync(StateHasChanged);
    }

    private void ExpandOrMoveToChild(AdmiralTreeSelectItem item)
    {
        if (!expandedIds.Contains(item.Id) && string.IsNullOrWhiteSpace(query))
        {
            expandedIds.Add(item.Id);
        }
        else
        {
            activeId = VisibleItems(item.Children).FirstOrDefault()?.Id ?? activeId;
        }
    }

    private void CollapseOrMoveToParent(AdmiralTreeSelectItem item)
    {
        if (!expandedIds.Remove(item.Id) || !string.IsNullOrWhiteSpace(query))
        {
            activeId = FindParent(Items, item.Id)?.Id ?? activeId;
        }
    }

    private IEnumerable<AdmiralTreeSelectItem> VisibleItems(IEnumerable<AdmiralTreeSelectItem> items)
    {
        foreach (var item in items.Where(item => !item.Disabled && IsVisible(item)))
        {
            yield return item;
            if (expandedIds.Contains(item.Id) || !string.IsNullOrWhiteSpace(query))
            {
                foreach (var child in VisibleItems(item.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private void ToggleExpanded(string id)
    {
        if (!expandedIds.Add(id))
        {
            expandedIds.Remove(id);
        }
    }
    private bool IsVisible(AdmiralTreeSelectItem item) => MatchesQuery(item, query);

    private async Task SelectAsync(AdmiralTreeSelectItem item)
    {
        if (IsInteractionBlocked || !Items.SelectMany(SelectableDescendants).Any(node => node.Id == item.Id))
        {
            return;
        }
        activeId = item.Id;
        if (!Multiple)
        {
            await ValueChanged.InvokeAsync(item.Id);
            Close();
            await trigger.FocusAsync(preventScroll: true);
            return;
        }
        var selected = new HashSet<string>(Values);
        NormalizeParents(Items, selected);
        var descendants = SelectableDescendants(item).ToArray();
        var checkedNow = descendants.All(node => selected.Contains(node.Id));
        foreach (var node in descendants)
        {
            if (checkedNow)
            {
                selected.Remove(node.Id);
            }
            else
            {
                selected.Add(node.Id);
            }
        }
        NormalizeParents(Items, selected);
        await ValuesChanged.InvokeAsync(selected.ToArray());
    }

    private async Task ClearAsync()
    {
        if (IsInteractionBlocked)
        {
            return;
        }
        if (Multiple)
        {
            await ValuesChanged.InvokeAsync(Array.Empty<string>());
        }
        else
        {
            await ValueChanged.InvokeAsync(null);
        }
        await trigger.FocusAsync(preventScroll: true);
    }

    private bool CanRemove(AdmiralTreeSelectItem item) => !IsInteractionBlocked
        && Items.SelectMany(SelectableDescendants).Any(node => node.Id == item.Id);

    private async Task RemoveChipAsync(AdmiralTreeSelectItem item)
    {
        if (!CanRemove(item))
        {
            return;
        }
        var selected = new HashSet<string>(Values);
        foreach (var node in SelectableDescendants(item))
        {
            selected.Remove(node.Id);
        }
        NormalizeParents(Items, selected);
        await ValuesChanged.InvokeAsync(selected.ToArray());
        await trigger.FocusAsync(preventScroll: true);
    }

    public async ValueTask DisposeAsync()
    {
        if (interop is not null)
        {
            await interop.DisposeAsync();
        }
    }
}
