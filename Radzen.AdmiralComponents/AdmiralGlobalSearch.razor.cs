using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralGlobalSearch : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private static long nextId;
    private readonly string instanceId = $"admiral-global-search-{Interlocked.Increment(ref nextId)}";
    private string query = string.Empty;
    private bool open;
    private int activeIndex = -1;
    private string? lastValue;
    private ElementReference root;
    private ElementReference input;
    private Internal.AdmiralComboBoxInterop<AdmiralGlobalSearch>? interop;

    [Parameter]
    public IReadOnlyList<AdmiralSearchItem> Items { get; set; } = [];

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<AdmiralSearchItem> ItemSelected { get; set; }

    [Parameter]
    public EventCallback<string> SearchSubmitted { get; set; }

    [Parameter]
    public string? Prefix { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = "Search";

    [Parameter]
    public string ClearAriaLabel { get; set; } = "Clear search";

    [Parameter]
    public string SubmitAriaLabel { get; set; } = "Search";

    [Parameter]
    public string EmptyText { get; set; } = "No matches";

    [Parameter]
    public int MaxResults { get; set; } = 5;

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string ResultsId => $"{instanceId}-results";
    private string? ActiveDescendant => open && activeIndex >= 0 && activeIndex < Results.Count
        ? OptionId(activeIndex)
        : null;
    private string CssClass => string.Join(' ', new[] { "admiral-global-search", Class }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
    private IReadOnlyList<AdmiralSearchItem> Results { get; set; } = [];

    protected override void OnParametersSet()
    {
        if (lastValue != Value)
        {
            query = Value ?? string.Empty;
            lastValue = Value;
            activeIndex = -1;
        }
        RefreshResults();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        interop ??= new(JS, this, "search");
        await interop.ConnectAsync(root);
    }

    private string OptionId(int index) => $"{instanceId}-option-{index}";

    private void RefreshResults()
    {
        Results = Items
            .Where(item => string.IsNullOrWhiteSpace(query)
                || item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (item.Meta?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(Math.Max(0, MaxResults))
            .ToArray();

        if (activeIndex >= Results.Count)
        {
            activeIndex = -1;
        }
    }

    private void SetQuery(string value)
    {
        query = value;
        activeIndex = -1;
        RefreshResults();
    }

    private void Open(FocusEventArgs _) => SetOpen(true);

    private async Task HandleInput(ChangeEventArgs args)
    {
        SetQuery(args.Value?.ToString() ?? string.Empty);
        SetOpen(true);
        await ValueChanged.InvokeAsync(query);
    }

    private async Task Clear()
    {
        SetQuery(string.Empty);
        SetOpen(true);
        await ValueChanged.InvokeAsync(query);
        await input.FocusAsync(preventScroll: true);
    }

    private async Task Submit()
    {
        SetOpen(false);
        await SearchSubmitted.InvokeAsync(query);
    }

    private async Task Select(AdmiralSearchItem item)
    {
        SetQuery(item.Title);
        SetOpen(false);
        await ValueChanged.InvokeAsync(query);
        await ItemSelected.InvokeAsync(item);
    }

    [JSInvokable]
    public Task DismissPopup() => InvokeAsync(() =>
    {
        SetOpen(false);
        StateHasChanged();
    });

    [JSInvokable]
    public async Task HandlePopupKey(string key, bool fromSearch)
    {
        var count = Results.Count;
        if (key == "ArrowDown" && count > 0)
        {
            SetOpen(true);
            activeIndex = (activeIndex + 1 + count) % count;
        }
        else if (key == "ArrowUp" && count > 0)
        {
            SetOpen(true);
            activeIndex = activeIndex < 0 ? count - 1 : (activeIndex - 1 + count) % count;
        }
        else if (key == "Escape")
        {
            SetOpen(false);
        }
        else if (key == "Enter")
        {
            if (open && activeIndex >= 0 && activeIndex < count)
            {
                await Select(Results[activeIndex]);
            }
            else
            {
                await Submit();
            }
        }
        await InvokeAsync(StateHasChanged);
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (!open)
        {
            activeIndex = -1;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (interop is not null)
        {
            await interop.DisposeAsync();
        }
    }
}
