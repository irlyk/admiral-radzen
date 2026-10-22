using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Admiral.Components.Internal;

// Only browser event semantics live in JS (conditional preventDefault and
// relatedTarget). Selection, expansion and callbacks remain in the component.
internal sealed class AdmiralComboBoxInterop<T>(IJSRuntime js, T owner, string mode) : IAsyncDisposable where T : class
{
    private IJSObjectReference? module;
    private IJSObjectReference? connection;
    private DotNetObjectReference<T>? reference;

    public async Task ConnectAsync(ElementReference root)
    {
        if (connection is not null) return;
        module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-combobox.js");
        reference = DotNetObjectReference.Create(owner);
        connection = await module.InvokeAsync<IJSObjectReference>("connect", root, reference, mode);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (connection is not null)
            {
                await connection.InvokeVoidAsync("dispose");
                await connection.DisposeAsync();
            }
            if (module is not null) await module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        finally { reference?.Dispose(); }
    }
}
