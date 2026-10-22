using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Internal;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralDrawer
{
    private const int TransitionDuration = 300;
    private static long nextId;
    private readonly string instanceId = $"admiral-drawer-{Interlocked.Increment(ref nextId)}";
    private ElementReference drawerElement;
    private AdmiralFocusScope? focusScope;
    private CancellationTokenSource? transitionCancellation;
    private bool rendered;
    private bool wasOpen;
    private bool requestingClose;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>Controlled open state. Use with OpenChanged or @bind-Open.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Requests a change of the controlled open state.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Heading used as the accessible name of the drawer.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Opening edge. Defaults to Right; Mobile always uses Right.</summary>
    [Parameter]
    public AdmiralDrawerPosition Position { get; set; } = AdmiralDrawerPosition.Right;

    /// <summary>Neutral background variant. Defaults to Primary.</summary>
    [Parameter]
    public AdmiralDrawerAppearance Appearance { get; set; } = AdmiralDrawerAppearance.Primary;

    /// <summary>CSS width, constrained to the viewport minus 16px. Defaults to 30rem.</summary>
    [Parameter]
    public string Width { get; set; } = "30rem";

    /// <summary>Forces the right-hand, near-full-width mobile composition.</summary>
    [Parameter]
    public bool Mobile { get; set; }

    /// <summary>Shows the modal backdrop and blocks background scrolling. Defaults to true.</summary>
    [Parameter]
    public bool Backdrop { get; set; } = true;

    [Parameter]
    public bool ShowClose { get; set; } = true;

    /// <summary>Allows clicking the modal backdrop to request closing. Defaults to true.</summary>
    [Parameter]
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Allows Escape inside the drawer to request closing. Defaults to true.</summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Disables restoring focus to the opener when the drawer closes.</summary>
    [Parameter]
    public bool PreventFocusRestore { get; set; }

    [Parameter]
    public string CloseAriaLabel { get; set; } = "Close drawer";

    /// <summary>Raised once when the close button, Escape or backdrop requests closing.</summary>
    [Parameter]
    public EventCallback Closed { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string TitleId => $"{instanceId}-title";
    private string DrawerStyle => $"--admiral-drawer-width:{Width}";
    private string BackdropClass => string.Join(' ', new[]
    {
        "admiral-drawer-backdrop",
        Backdrop ? "admiral-drawer-backdrop--modal" : "admiral-drawer-backdrop--nonmodal",
        Open ? null : "admiral-drawer-backdrop--closing"
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private string CssClass => string.Join(' ', new[]
    {
        "admiral-drawer",
        $"admiral-drawer--{(Mobile ? AdmiralDrawerPosition.Right : Position).ToString().ToLowerInvariant()}",
        $"admiral-drawer--{Appearance.ToString().ToLowerInvariant()}",
        Mobile ? "admiral-drawer--mobile" : null,
        Open ? null : "admiral-drawer--closing",
        Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    protected override void OnParametersSet()
    {
        if (Open == wasOpen)
        {
            return;
        }

        wasOpen = Open;
        transitionCancellation?.Cancel();
        transitionCancellation?.Dispose();
        transitionCancellation = null;

        if (Open)
        {
            rendered = true;
        }
        else
        {
            transitionCancellation = new();
            _ = FinishClosingAsync(transitionCancellation.Token);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        focusScope ??= new(JS);
        await focusScope.UpdateAsync(drawerElement, Open, !PreventFocusRestore);
    }

    private async Task FinishClosingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TransitionDuration, cancellationToken);
            await InvokeAsync(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    rendered = false;
                    StateHasChanged();
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Reopening or disposal cancels the previous exit animation.
        }
    }

    private Task HandleBackdropClick() => Backdrop && CloseOnBackdropClick ? CloseAsync() : Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args) =>
        CloseOnEscape && args.Key == "Escape" ? CloseAsync() : Task.CompletedTask;

    private async Task CloseAsync()
    {
        if (!Open || requestingClose)
        {
            return;
        }

        requestingClose = true;
        try
        {
            await OpenChanged.InvokeAsync(false);
            await Closed.InvokeAsync();
        }
        finally
        {
            requestingClose = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        transitionCancellation?.Cancel();
        transitionCancellation?.Dispose();
        if (focusScope is not null)
        {
            await focusScope.DisposeAsync();
        }
    }
}
