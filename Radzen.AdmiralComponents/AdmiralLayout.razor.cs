using Microsoft.AspNetCore.Components;
using Radzen.Admiral.Components.Internal;
using Radzen.Admiral.Components.Models;

namespace Radzen.Admiral.Components;

public partial class AdmiralLayout
{

    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter]
    public RenderFragment? SideMenu { get; set; }

    [Parameter]
    public RenderFragment? Content { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool HasSideMenu { get; set; } = true;

    [Parameter]
    public string HeaderHeight { get; set; } = "4rem";

    [Parameter]
    public AdmiralSideMenuWidth SideMenuWidth { get; set; } = AdmiralSideMenuWidth.Small;

    [Parameter]
    public AdmiralSideMenuState SideMenuState { get; set; } = AdmiralSideMenuState.Expanded;

    [Parameter]
    public EventCallback<AdmiralSideMenuState> SideMenuStateChanged { get; set; }

    [Parameter]
    public AdmiralSideMenuState DesktopCollapseTarget { get; set; } = AdmiralSideMenuState.IconsOnly;

    [Parameter]
    public bool MenuOpen { get; set; }

    [Parameter]
    public EventCallback<bool> MenuOpenChanged { get; set; }

    [Parameter]
    public string SideMenuId { get; set; } = "admiral-side-menu";

    [Parameter]
    public string SideMenuAriaLabel { get; set; } = "Main navigation";

    [Parameter]
    public string CloseMenuAriaLabel { get; set; } = "Close navigation";

    [Parameter]
    public bool SideMenuShadow { get; set; }

    [Parameter]
    public string? HeaderClass { get; set; }

    [Parameter]
    public string? HeaderStyle { get; set; }

    [Parameter]
    public string? SideMenuClass { get; set; }

    [Parameter]
    public string? SideMenuStyle { get; set; }

    [Parameter]
    public string? BodyClass { get; set; }

    [Parameter]
    public string? BodyStyle { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // Compatibility aliases for the first packaged layout API.

    [Parameter]
    public AdmiralSideMenuCollapseMode DesktopCollapseMode { get; set; } = AdmiralSideMenuCollapseMode.IconsOnly;

    [Parameter]
    public bool SideMenuCollapsed { get; set; }

    [Parameter]
    public EventCallback<bool> SideMenuCollapsedChanged { get; set; }

    private AdmiralLayoutContext layoutContext = default!;

    protected override void OnInitialized()
    {
        layoutContext = new()
        {
            ToggleOverlayAsync = ToggleMenu,
            CloseOverlayAsync = CloseMenu,
            ToggleDesktopAsync = ToggleDesktopNavigation,
            IsOverlayOpen = () => MenuOpen,
            GetSideMenuState = () => EffectiveSideMenuState,
            SideMenuId = SideMenuId
        };
    }

    public Task ToggleMenu() => SetMenuOpen(!MenuOpen);
    public Task CloseMenu() => SetMenuOpen(false);
    public Task ToggleSideMenuCollapsed() => ToggleDesktopNavigation();

    private AdmiralSideMenuState EffectiveSideMenuState => SideMenuState != AdmiralSideMenuState.Expanded
        ? SideMenuState
        : SideMenuCollapsed
            ? (DesktopCollapseMode == AdmiralSideMenuCollapseMode.IconsOnly ? AdmiralSideMenuState.IconsOnly : AdmiralSideMenuState.Hidden)
            : AdmiralSideMenuState.Expanded;

    private async Task ToggleDesktopNavigation()
    {
        var next = EffectiveSideMenuState == AdmiralSideMenuState.Expanded
            ? (DesktopCollapseTarget == AdmiralSideMenuState.Expanded ? AdmiralSideMenuState.IconsOnly : DesktopCollapseTarget)
            : AdmiralSideMenuState.Expanded;
        SideMenuCollapsed = next != AdmiralSideMenuState.Expanded;
        SideMenuState = next;
        await SideMenuCollapsedChanged.InvokeAsync(SideMenuCollapsed);
        await SideMenuStateChanged.InvokeAsync(next);
    }

    private async Task SetMenuOpen(bool value)
    {
        if (MenuOpen == value)
        {
            return;
        }
        MenuOpen = value;
        await MenuOpenChanged.InvokeAsync(value);
    }

    private string CssClass => string.Join(' ', new[]
    {
        "admiral-layout",
        HasSideMenu ? "admiral-layout--with-side-menu" : "admiral-layout--without-side-menu",
        MenuOpen ? "admiral-layout--menu-open" : null,
        SideMenuShadow ? "admiral-layout--side-menu-shadow" : null,
        $"admiral-layout--side-menu-{EffectiveSideMenuState.ToString().ToLowerInvariant()}",
        Class
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private string CssVariables =>
        $"--admiral-layout-side-menu-width:{(int)SideMenuWidth}px;" +
        $"--admiral-layout-header-height:{(Header is null ? "0px" : HeaderHeight)}";

    private string CombinedStyle => $"{CssVariables};{Style}";
    private string SideMenuCssClass => string.Join(' ', new[] { "admiral-layout__side-menu", SideMenuClass }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
    private string HeaderCssClass => string.Join(' ', new[] { "admiral-layout__header", HeaderClass }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
    private string BodyCssClass => string.Join(' ', new[] { "admiral-layout__body", BodyClass }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}
