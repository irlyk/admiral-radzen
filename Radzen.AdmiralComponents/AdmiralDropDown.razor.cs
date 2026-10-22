using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralDropDown<TValue, TItem> : IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    private RadzenDropDown<TValue>? dropDown;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-select-focus.js");
            await module.InvokeVoidAsync("installEscapeFocusRestore");
            await module.DisposeAsync();
        }
    }

    private async Task FocusTriggerBeforeOpenAsync()
    {
        if (dropDown is not null)
        {
            await dropDown.FocusAsync();
        }
    }

    private IReadOnlyList<OptionView> localOptions = [];
    private IReadOnlyList<OptionView>? asyncOptions;
    private OptionView? asyncSelectedOption;
    private TItem[] localItems = [];
    private CancellationTokenSource? loadCancellation;
    private LoadDataArgs? lastLoadArgs;
    private Exception? loadError;
    private Task? activeLoadTask;
    private string? activeLoadKey;
    private string? completedLoadKey;
    private int totalCount;
    private long loadVersion;
    private bool isLoading;

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    [Parameter]
    public System.Linq.Expressions.Expression<Func<TValue?>>? ValueExpression { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public IEnumerable<TItem>? Data { get; set; }

    [Parameter]
    public Func<AdmiralDropDownQuery, CancellationToken, Task<AdmiralDropDownResult<TItem>>>? DataProvider { get; set; }

    [Parameter]
    public Func<TItem, string>? TextSelector { get; set; }

    [Parameter]
    public Func<TItem, TValue?>? ValueSelector { get; set; }

    [Parameter]
    public Func<TItem, string?>? GroupSelector { get; set; }

    [Parameter]
    public Func<string, string>? GroupTextSelector { get; set; }

    [Parameter]
    public Func<string, bool>? GroupDisabledSelector { get; set; }

    [Parameter]
    public Func<TItem, bool>? DisabledSelector { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter]
    public RenderFragment<TItem>? SelectedTemplate { get; set; }

    [Parameter]
    public EventCallback<Exception> LoadFailed { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? FilterPlaceholder { get; set; }

    [Parameter]
    public string LoadingText { get; set; } = "Loading…";

    [Parameter]
    public string EmptyText { get; set; } = "No options";

    [Parameter]
    public string ErrorText { get; set; } = "Could not load options";

    [Parameter]
    public string RetryText { get; set; } = "Retry";

    [Parameter]
    public string ClearAriaLabel { get; set; } = "Clear";

    [Parameter]
    public string SearchAriaLabel { get; set; } = "Search options";

    [Parameter]
    public string EmptyAriaLabel { get; set; } = "Empty";

    [Parameter]
    public string SelectAllText { get; set; } = "Select all";

    [Parameter]
    public string SelectedItemsText { get; set; } = "items selected";

    [Parameter]
    public string RemoveChipTitle { get; set; } = "Remove";

    [Parameter]
    public bool AllowFiltering { get; set; } = true;

    [Parameter]
    public bool AllowClear { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public bool Chips { get; set; }

    [Parameter]
    public bool AllowSelectAll { get; set; } = true;

    [Parameter]
    public bool AllowVirtualization { get; set; }

    [Parameter]
    public bool ClearSearchAfterSelection { get; set; } = true;

    [Parameter]
    public int FilterDelay { get; set; } = 300;

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public int VirtualizationOverscanCount { get; set; } = 5;

    [Parameter]
    public string PopupStyle { get; set; } = "max-height: 15rem; overflow-x: hidden";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string ShellClass => string.Join(' ', new[]
        {
            "admiral-dropdown-shell",
            isLoading ? "admiral-dropdown-shell--loading" : null
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private string DropDownClass => string.Join(' ', new[] { "admiral-dropdown", Class }
        .Where(value => !string.IsNullOrWhiteSpace(value)));

    private IReadOnlyDictionary<string, object>? EffectiveAsyncInputAttributes
    {
        get
        {
            if (InputAttributes?.ContainsKey("aria-label") == true || asyncSelectedOption is null || Value is null)
            {
                return InputAttributes;
            }

            var attributes = InputAttributes is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(InputAttributes);
            attributes["aria-label"] = asyncSelectedOption.Text;
            return attributes;
        }
    }

    private string EffectiveAsyncEmptyAriaLabel => asyncSelectedOption is not null && Value is not null
        ? asyncSelectedOption.Text
        : EmptyAriaLabel;

    protected override void OnParametersSet()
    {
        if (DataProvider is not null)
        {
            if (Value is null || asyncSelectedOption is not null && !object.Equals(asyncSelectedOption.Value, Value))
            {
                asyncSelectedOption = null;
            }

            var currentSelection = asyncOptions?.FirstOrDefault(option => !option.IsGroupHeader && object.Equals(option.Value, Value));
            if (currentSelection is not null)
            {
                asyncSelectedOption = currentSelection;
            }

            return;
        }

        var incoming = Data?.ToArray() ?? [];
        if (!HasSameItems(localItems, incoming))
        {
            localItems = incoming;
            localOptions = BuildOptions(incoming);
            return;
        }

        var refreshed = BuildOptions(incoming);
        if (refreshed.Count != localOptions.Count)
        {
            localOptions = refreshed;
            return;
        }

        for (var index = 0; index < localOptions.Count; index++)
        {
            localOptions[index].RefreshFrom(refreshed[index]);
        }
    }

    private Task HandleLoadDataAsync(LoadDataArgs args)
    {
        if (DataProvider is null)
        {
            return Task.CompletedTask;
        }

        var request = new LoadDataArgs { Filter = args.Filter, Skip = args.Skip, Top = args.Top };
        var requestKey = $"{request.Filter}\u001f{request.Skip}\u001f{request.Top}";
        if (activeLoadTask is { IsCompleted: false } && activeLoadKey == requestKey)
        {
            return Task.CompletedTask;
        }

        if (completedLoadKey == requestKey)
        {
            return Task.CompletedTask;
        }

        lastLoadArgs = request;
        activeLoadKey = requestKey;
        completedLoadKey = null;
        var version = ++loadVersion;
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        loadCancellation = new CancellationTokenSource();
        activeLoadTask = LoadDataCoreAsync(request, requestKey, version, loadCancellation.Token, DataProvider);
        return activeLoadTask;
    }

    private async Task LoadDataCoreAsync(LoadDataArgs args, string requestKey, long version, CancellationToken cancellationToken,
        Func<AdmiralDropDownQuery, CancellationToken, Task<AdmiralDropDownResult<TItem>>> provider)
    {
        isLoading = true;
        loadError = null;
        asyncOptions = [];
        await InvokeAsync(StateHasChanged);

        try
        {
            var query = new AdmiralDropDownQuery(args.Filter ?? string.Empty, args.Skip ?? 0, args.Top);
            var result = await provider(query, cancellationToken);
            if (cancellationToken.IsCancellationRequested || version != loadVersion)
            {
                return;
            }

            asyncOptions = BuildOptions(result.Items);
            totalCount = result.TotalCount;
            var loadedSelection = asyncOptions.FirstOrDefault(option => !option.IsGroupHeader && object.Equals(option.Value, Value));
            if (loadedSelection is not null)
            {
                asyncSelectedOption = loadedSelection;
            }
            completedLoadKey = requestKey;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            if (version != loadVersion)
            {
                return;
            }

            loadError = exception;
            asyncOptions = [];
            totalCount = 0;
            completedLoadKey = requestKey;
            await LoadFailed.InvokeAsync(exception);
        }
        finally
        {
            if (version == loadVersion)
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task RetryAsync()
    {
        if (lastLoadArgs is not null)
        {
            activeLoadKey = null;
            completedLoadKey = null;
            await HandleLoadDataAsync(lastLoadArgs);
        }
    }

    private IReadOnlyList<OptionView> BuildOptions(IEnumerable<TItem> source)
    {
        var items = source.ToArray();
        var result = new List<OptionView>(items.Length);
        string? previousGroup = null;

        foreach (var item in items)
        {
            var group = GroupSelector?.Invoke(item);
            if (!string.IsNullOrWhiteSpace(group) && !string.Equals(group, previousGroup, StringComparison.Ordinal))
            {
                var groupLabel = GroupTextSelector?.Invoke(group) ?? group;
                var groupSearchText = string.Join(' ', items
                    .Where(candidate => string.Equals(GroupSelector?.Invoke(candidate), group, StringComparison.Ordinal))
                    .Select(GetText));
                result.Add(OptionView.GroupHeader(group, groupLabel, groupSearchText));
            }

            var groupDisabled = !string.IsNullOrWhiteSpace(group) && GroupDisabledSelector?.Invoke(group) == true;
            result.Add(OptionView.ItemOption(item, GetText(item), GetValue(item), groupDisabled || DisabledSelector?.Invoke(item) == true));
            previousGroup = group;
        }

        return result;
    }

    private string GetText(TItem item) => TextSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;
    private TValue? GetValue(TItem item)
    {
        if (ValueSelector is not null)
        {
            return ValueSelector(item);
        }

        if (item is TValue value)
        {
            return value;
        }

        throw new InvalidOperationException($"{nameof(ValueSelector)} is required when {typeof(TItem).Name} is not assignable to {typeof(TValue).Name}.");
    }

    private static bool HasSameItems(IReadOnlyList<TItem> current, IReadOnlyList<TItem> incoming)
    {
        if (current.Count != incoming.Count)
        {
            return false;
        }

        for (var index = 0; index < current.Count; index++)
        {
            if (!EqualityComparer<TItem>.Default.Equals(current[index], incoming[index]))
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
    }

    private sealed class OptionView
    {
        public TItem? Item
        {
            get; private set;
        }
        public TValue? Value
        {
            get; set;
        }
        public string Text { get; set; } = string.Empty;
        public string? GroupKey
        {
            get; private set;
        }
        public string? GroupLabel
        {
            get; private set;
        }
        public bool IsGroupHeader
        {
            get; private set;
        }
        public bool IsDisabled
        {
            get; set;
        }

        public static OptionView GroupHeader(string key, string label, string searchText) => new()
        {
            Value = default,
            Text = $"{label} {searchText}",
            GroupKey = key,
            GroupLabel = label,
            IsGroupHeader = true,
            IsDisabled = true
        };

        public static OptionView ItemOption(TItem item, string text, TValue? value, bool disabled) => new()
        {
            Item = item,
            Value = value,
            Text = text,
            IsDisabled = disabled
        };

        public void RefreshFrom(OptionView source)
        {
            Item = source.Item;
            Value = source.Value;
            Text = source.Text;
            GroupKey = source.GroupKey;
            GroupLabel = source.GroupLabel;
            IsGroupHeader = source.IsGroupHeader;
            IsDisabled = source.IsDisabled;
        }
    }
}
