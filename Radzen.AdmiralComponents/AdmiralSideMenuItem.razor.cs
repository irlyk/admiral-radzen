using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Radzen.Admiral.Components.Internal;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralSideMenuItem
{

    [CascadingParameter]
    internal AdmiralLayoutContext? Layout { get; set; }

    [CascadingParameter(Name = "AdmiralMenuDepth")]
    public int Depth { get; set; }

    [Parameter]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    [Parameter]
    public RenderFragment? Badge { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string ExpandAriaLabel { get; set; } = "Toggle nested navigation";

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private readonly string childrenId = $"admiral-side-menu-children-{Guid.NewGuid():N}";
    private bool expanded;
    private bool initialized;
    private bool HasChildren => ChildContent is not null;
    private string EffectiveAriaLabel => AriaLabel ?? Text;

    private RenderFragment ItemContent => builder =>
    {
        var sequence = 0;
        if (!string.IsNullOrWhiteSpace(Icon))
        {
            builder.OpenComponent<AdmiralIcon>(sequence++);
            builder.AddAttribute(sequence++, nameof(AdmiralIcon.Icon), Icon);
            builder.AddAttribute(sequence++, "class", "admiral-side-menu-item__icon");
            builder.CloseComponent();
        }
        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "admiral-side-menu-item__label");
        builder.AddContent(sequence++, Text);
        builder.CloseElement();
        if (Badge is not null)
        {
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "admiral-side-menu-item__badge");
            builder.AddContent(sequence++, Badge);
            builder.CloseElement();
        }
        if (HasChildren && string.IsNullOrWhiteSpace(Href))
        {
            builder.OpenComponent<AdmiralIcon>(sequence++);
            builder.AddAttribute(sequence++, nameof(AdmiralIcon.Icon), "admiral:system/chevron-down-outline");
            builder.AddAttribute(sequence++, "class", "admiral-side-menu-item__inline-expand");
            builder.CloseComponent();
        }
    };

    protected override void OnParametersSet()
    {
        if (!initialized)
        {
            expanded = Expanded;
            initialized = true;
        }
        else if (Expanded != expanded && ExpandedChanged.HasDelegate)
        {
            expanded = Expanded;
        }
    }

    private async Task ToggleExpandedAsync()
    {
        if (Disabled || !HasChildren)
        {
            return;
        }
        expanded = !expanded;
        await ExpandedChanged.InvokeAsync(expanded);
    }

    private Task HandleNavigateAsync() => Disabled || Layout is null
        ? Task.CompletedTask
        : Layout.CloseOverlayAsync();

    private string CssClass => string.Join(' ', new[]
    {
        "admiral-side-menu-item",
        expanded ? "admiral-side-menu-item--expanded" : null,
        HasChildren ? "admiral-side-menu-item--parent" : null,
        Disabled ? "admiral-side-menu-item--disabled" : null,
        Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
