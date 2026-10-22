using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Internal;

namespace Radzen.Admiral.Components;

public partial class AdmiralMenu : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment? Trigger { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public bool MatchTriggerWidth { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = "Menu";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? TriggerClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private readonly AdmiralMenuContext context;
    private readonly string popupId = $"admiral-menu-{Guid.NewGuid():N}";
    private ElementReference anchor;
    private IJSObjectReference? module;
    private DotNetObjectReference<AdmiralMenu>? dotNetReference;
    private bool connectedOpen;

    public AdmiralMenu()
    {
        context = new()
        {
            CloseAsync = CloseAsync
        };
    }

    private Task ToggleAsync() => Open ? CloseAsync() : OpenAsync();

    private async Task OpenAsync()
    {
        Open = true;
        await OpenChanged.InvokeAsync(true);
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-menu.js?v=20261008");
            dotNetReference = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("connectTrigger", anchor, popupId);
        }
        if (module is null || connectedOpen == Open)
        {
            return;
        }
        connectedOpen = Open;
        if (Open)
        {
            await module.InvokeVoidAsync("connectOutsideClick", anchor, popupId, dotNetReference);
        }
        else
        {
            await module.InvokeVoidAsync("disconnectOutsideClick", popupId);
        }
    }

    public Task CloseAsync() => CloseCoreAsync(restoreFocus: true);

    private async Task CloseCoreAsync(bool restoreFocus)
    {
        if (!Open)
        {
            return;
        }
        Open = false;
        await OpenChanged.InvokeAsync(false);
        if (module is not null && restoreFocus)
        {
            await module.InvokeVoidAsync("focusTrigger", anchor);
        }
        await InvokeAsync(StateHasChanged);
    }
    [JSInvokable]
    public Task CloseFromDocumentAsync(bool restoreFocus) => CloseCoreAsync(restoreFocus);

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("disconnectOutsideClick", popupId);
                await module.InvokeVoidAsync("disconnectTrigger", popupId);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        dotNetReference?.Dispose();
    }
    private string TriggerCssClass => string.Join(' ', new[] { "admiral-menu__trigger", TriggerClass }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private string MenuCssClass => string.Join(' ', new[] { "admiral-menu", Class }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private string PopupCssClass => MatchTriggerWidth
        ? "admiral-menu__popup admiral-menu__popup--match-trigger"
        : "admiral-menu__popup";
}
