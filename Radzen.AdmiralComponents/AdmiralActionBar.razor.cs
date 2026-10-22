using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralActionBar : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>Actions in display order. Narrow layouts move a suffix into the menu.</summary>
    [Parameter]
    public IReadOnlyList<AdmiralActionItem> Items { get; set; } = [];

    /// <summary>Actions always shown in the menu, after automatically hidden actions.</summary>
    [Parameter]
    public IReadOnlyList<AdmiralActionItem> OverflowItems { get; set; } = [];

    /// <summary>Raised for enabled actions from either the bar or its menu.</summary>
    [Parameter]
    public EventCallback<AdmiralActionItem> ActionClicked { get; set; }

    /// <summary>Control size. Defaults to ExtraLarge (56px); L/M/S are 48/40/32px.</summary>
    [Parameter]
    public AdmiralActionBarSize Size { get; set; } = AdmiralActionBarSize.ExtraLarge;

    /// <summary>Fit actions to the available width. Defaults to true. False retains manual overflow.</summary>
    [Parameter]
    public bool AutoOverflow { get; set; } = true;

    /// <summary>Accessible toolbar name. Defaults to Actions.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Actions";

    /// <summary>Accessible menu trigger name. Defaults to More actions.</summary>
    [Parameter]
    public string OverflowAriaLabel { get; set; } = "More actions";

    /// <summary>Menu trigger icon. Defaults to Admiral's horizontal ellipsis.</summary>
    [Parameter]
    public string OverflowIcon { get; set; } = "admiral:system/more-horizontal-outline";

    /// <summary>Additional CSS classes.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Attributes forwarded to the toolbar element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private ElementReference element;
    private IJSObjectReference? module;
    private IJSObjectReference? observer;
    private DotNetObjectReference<AdmiralActionBar>? selfReference;
    private readonly SemaphoreSlim renderGate = new(1, 1);
    private IReadOnlyList<AdmiralActionItem> visibleItems = [];
    private IReadOnlyList<AdmiralActionItem> overflowItems = [];
    private double availableWidth = double.PositiveInfinity;
    private bool overflowOpen;
    private bool disposed;

    private AdmiralButtonDimension ButtonDimension => Size switch
    {
        AdmiralActionBarSize.Small => AdmiralButtonDimension.S,
        AdmiralActionBarSize.Medium => AdmiralButtonDimension.M,
        AdmiralActionBarSize.Large => AdmiralButtonDimension.L,
        _ => AdmiralButtonDimension.Xl
    };

    private int ButtonWidth => Size switch
    {
        AdmiralActionBarSize.Small => 32,
        AdmiralActionBarSize.Medium => 40,
        AdmiralActionBarSize.Large => 48,
        _ => 56
    };

    private string CssClass => string.Join(' ', new[]
    {
        "admiral-action-bar",
        $"admiral-action-bar--{ButtonDimension.ToString().ToLowerInvariant()}",
        AutoOverflow ? "admiral-action-bar--adaptive" : null,
        Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    protected override void OnParametersSet() => PartitionItems();

    private void PartitionItems()
    {
        var count = Items.Count;
        if (AutoOverflow && double.IsFinite(availableWidth))
        {
            var total = Items.Select((item, index) => ItemWidth(item, index)).Sum();
            var needsMenu = OverflowItems.Count > 0 || total > availableWidth + 0.5;
            var remaining = Math.Max(0, availableWidth - (needsMenu ? ButtonWidth : 0));
            count = 0;
            foreach (var item in Items)
            {
                var width = ItemWidth(item, count);
                if (width > remaining + 0.5)
                {
                    break;
                }
                remaining -= width;
                count++;
            }
        }
        visibleItems = Items.Take(count).ToArray();
        overflowItems = Items.Skip(count).Concat(OverflowItems).ToArray();
        if (overflowItems.Count == 0)
        {
            overflowOpen = false;
        }
    }

    private int ItemWidth(AdmiralActionItem item, int index) => ButtonWidth
        + (index > 0 && item.DividerBefore ? Size == AdmiralActionBarSize.Small ? 16 : 20 : 0);

    [JSInvokable]
    public Task UpdateAvailableWidth(double width) => InvokeAsync(() =>
    {
        if (disposed || !double.IsFinite(width) || width < 0 || Math.Abs(width - availableWidth) < 0.5)
        {
            return;
        }
        availableWidth = width;
        var previous = visibleItems.Count;
        PartitionItems();
        if (previous != visibleItems.Count)
        {
            overflowOpen = false;
            StateHasChanged();
        }
    });

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await renderGate.WaitAsync();
        try
        {
            if (disposed)
            {
                return;
            }
            module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-action-bar.js");
            selfReference ??= DotNetObjectReference.Create(this);
            observer ??= await module.InvokeAsync<IJSObjectReference>("connect", element, selfReference);
            await observer.InvokeVoidAsync("update", AutoOverflow);
        }
        finally
        {
            renderGate.Release();
        }
    }

    private Task InvokeAction(AdmiralActionItem item) => item.Disabled
        ? Task.CompletedTask
        : ActionClicked.InvokeAsync(item);

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        await renderGate.WaitAsync();
        try
        {
            if (observer is not null)
            {
                await observer.InvokeVoidAsync("dispose");
                await observer.DisposeAsync();
            }
            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        finally
        {
            selfReference?.Dispose();
            renderGate.Release();
        }
    }
}
