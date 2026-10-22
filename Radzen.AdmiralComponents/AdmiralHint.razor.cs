using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Admiral.Components.Models;
using Radzen.Admiral.Components.Internal;

namespace Radzen.Admiral.Components;

public partial class AdmiralHint
{
    private readonly string popupId = $"admiral-hint-{Guid.NewGuid():N}";
    private ElementReference anchor;
    private ElementReference popupElement;
    private AdmiralFocusScope? focusScope;
    private IJSObjectReference? module;
    private DotNetObjectReference<AdmiralHint>? reference;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>Label for the default trigger; not required with TriggerContent.</summary>
    [Parameter]
    public string TriggerText { get; set; } = string.Empty;
    [Parameter]
    public string TriggerIcon { get; set; } = "admiral:service/help-outline";
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    /// <summary>Custom trigger. Include a keyboard-focusable element.</summary>
    [Parameter]
    public RenderFragment? TriggerContent { get; set; }
    [Parameter]
    public AdmiralHintSize Size { get; set; } = AdmiralHintSize.Medium;
    /// <summary>Preferred side with automatic viewport fallback. Null preserves legacy Position.</summary>
    [Parameter]
    public AdmiralHintPlacement? Placement { get; set; }
    /// <summary>Click preserves the original Blazor default; React defaults to Hover.</summary>
    [Parameter]
    public AdmiralHintTrigger VisibilityTrigger { get; set; } = AdmiralHintTrigger.Click;
    /// <summary>Open state. Supports @bind-Open, or internal state without a binding.</summary>
    [Parameter]
    public bool Open { get; set; }
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }
    /// <summary>Opt out of initial focus and the focus trap in click mode.</summary>
    [Parameter]
    public bool PreventFocusSteal { get; set; }
    /// <summary>Admiral appearance for the default trigger; null preserves legacy button parameters.</summary>
    [Parameter]
    public AdmiralButtonAppearance? TriggerAppearance { get; set; }
    /// <summary>Compatibility alias. Prefer Placement for new code.</summary>
    [Parameter]
    public TooltipPosition Position { get; set; } = TooltipPosition.Bottom;
    /// <summary>Compatibility parameter. Prefer TriggerAppearance or TriggerContent.</summary>
    [Parameter]
    public ButtonStyle TriggerButtonStyle { get; set; } = ButtonStyle.Primary;
    /// <summary>Compatibility parameter. Prefer TriggerAppearance or TriggerContent.</summary>
    [Parameter]
    public Variant TriggerVariant { get; set; } = Variant.Filled;
    [Parameter]
    public bool Disabled { get; set; }
    [Parameter]
    public bool CloseOnDocumentClick { get; set; } = true;
    [Parameter]
    public string CloseAriaLabel { get; set; } = "Close hint";
    [Parameter]
    public string? Class { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string TitleId => $"{popupId}-title";
    private string PopupClass => $"admiral-hint-popup admiral-hint--{Size.ToString().ToLowerInvariant()} admiral-hint-popup--{VisibilityTrigger.ToString().ToLowerInvariant()}";
    private AdmiralHintPlacement EffectivePlacement => Placement ?? Position switch
    {
        TooltipPosition.Top => AdmiralHintPlacement.Top,
        TooltipPosition.Right => AdmiralHintPlacement.Right,
        TooltipPosition.Left => AdmiralHintPlacement.Left,
        _ => AdmiralHintPlacement.Bottom
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Radzen.Admiral.Components/admiral-hint.js");
            reference = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("connect", popupId, anchor, reference);
        }
        if (module is not null)
        {
            await module.InvokeVoidAsync("update", popupId, new
            {
                open = Open && !Disabled,
                disabled = Disabled,
                hover = VisibilityTrigger == AdmiralHintTrigger.Hover,
                placement = EffectivePlacement.ToString().ToLowerInvariant(),
                closeOnDocumentClick = CloseOnDocumentClick
            });
            focusScope ??= new(JS);
            await focusScope.UpdateAsync(popupElement, Open && !Disabled &&
                VisibilityTrigger == AdmiralHintTrigger.Click && !PreventFocusSteal);
        }
    }

    /// <summary>Requests visibility from the trigger, card or browser interaction handlers.</summary>
    [JSInvokable]
    public async Task SetOpenAsync(bool value)
    {
        if (Open == value || (value && Disabled))
        {
            return;
        }
        Open = value;
        await OpenChanged.InvokeAsync(value);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (focusScope is not null)
        {
            await focusScope.DisposeAsync();
        }
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("disconnect", popupId);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        reference?.Dispose();
    }
}
