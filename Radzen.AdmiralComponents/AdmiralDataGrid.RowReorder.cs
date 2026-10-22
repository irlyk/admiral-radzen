using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace Radzen.Admiral.Components;

public partial class AdmiralDataGrid<TItem> where TItem : notnull
{
    private IJSObjectReference? rowReorderModule;
    private DotNetObjectReference<AdmiralDataGrid<TItem>>? dotNetReference;

    private async Task ConnectRowReorderAsync()
    {
        if (AllowRowReorder)
        {
            rowReorderModule ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Radzen.Admiral.Components/admiral-data-grid.js");
            dotNetReference ??= DotNetObjectReference.Create(this);
            await rowReorderModule.InvokeVoidAsync("connectRowReorder", rootElement, dotNetReference);
        }
    }

    private void HandleRowRender(RowRenderEventArgs<TItem> args)
    {
        RowRender?.Invoke(args);
        if (AllowRowReorder && args.Data is not null)
        {
            args.Attributes["data-admiral-row-key"] = GetRowKey(args.Data);
        }
    }

    [JSInvokable]
    public async Task HandleRowReorder(string rowKey, string targetRowKey, bool insertAfter)
    {
        var items = FilteredItems.ToList();
        var oldIndex = items.FindIndex(item => string.Equals(GetRowKey(item), rowKey, StringComparison.Ordinal));
        var targetIndex = items.FindIndex(item => string.Equals(GetRowKey(item), targetRowKey, StringComparison.Ordinal));
        if (oldIndex < 0 || targetIndex < 0 || oldIndex == targetIndex)
        {
            return;
        }

        var item = items[oldIndex];
        items.RemoveAt(oldIndex);
        targetIndex = items.FindIndex(candidate => string.Equals(GetRowKey(candidate), targetRowKey, StringComparison.Ordinal));
        var newIndex = Math.Clamp(targetIndex + (insertAfter ? 1 : 0), 0, items.Count);
        if (newIndex == oldIndex)
        {
            return;
        }

        var nextRowKey = newIndex < items.Count ? GetRowKey(items[newIndex]) : null;
        await RowReordered.InvokeAsync(new(item, oldIndex, newIndex, rowKey, nextRowKey));
    }

    private string GetRowKey(TItem item)
    {
        if (RowKey is not null)
        {
            return RowKey(item);
        }

        var id = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
        return !string.IsNullOrWhiteSpace(id) ? id : item.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task HandleRowReorderKeyDown(KeyboardEventArgs args, TItem item)
    {
        if (args.Key is not ("ArrowUp" or "ArrowDown"))
        {
            return;
        }

        var items = FilteredItems.ToList();
        var index = items.FindIndex(candidate => string.Equals(GetRowKey(candidate), GetRowKey(item), StringComparison.Ordinal));
        var targetIndex = args.Key == "ArrowUp" ? index - 1 : index + 1;
        if (index < 0 || targetIndex < 0 || targetIndex >= items.Count)
        {
            return;
        }

        await HandleRowReorder(GetRowKey(item), GetRowKey(items[targetIndex]), args.Key == "ArrowDown");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (rowReorderModule is not null)
            {
                await rowReorderModule.InvokeVoidAsync("disconnectRowReorder", rootElement);
                await rowReorderModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        dotNetReference?.Dispose();
    }
}
