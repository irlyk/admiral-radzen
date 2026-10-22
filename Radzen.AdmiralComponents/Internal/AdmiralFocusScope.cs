using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Admiral.Components.Internal;

// Shared lifecycle for modal surfaces which don't use RadzenDialog.
internal sealed class AdmiralFocusScope(IJSRuntime js) : IAsyncDisposable
{
    private readonly string id = Guid.NewGuid().ToString("N");
    private IJSObjectReference? module;
    private bool active;
    private bool restoreFocusEnabled = true;

    public async Task UpdateAsync(ElementReference element, bool open, bool restoreFocus = true)
    {
        if (active && module is not null && restoreFocusEnabled != restoreFocus)
        {
            await module.InvokeVoidAsync("setRestoreFocus", id, restoreFocus);
        }
        restoreFocusEnabled = restoreFocus;
        if (active == open)
        {
            return;
        }
        module ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-focus-scope.js");
        active = open;
        if (open) await module.InvokeVoidAsync("connect", id, element, restoreFocus);
        else await module.InvokeVoidAsync("disconnect", id);
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null) return;
        try
        {
            await module.InvokeVoidAsync("disconnect", id);
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }
}
