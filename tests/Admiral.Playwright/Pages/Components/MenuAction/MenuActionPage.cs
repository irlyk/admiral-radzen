namespace Admiral.Playwright.Pages.Components.MenuAction;

internal sealed class MenuActionPage(IPage page, string route, bool themeOnly = false) : GalleryPage(page)
{
    public ILocator Preview => Page.GetByTestId("menu-action-playground").Locator(".admiral-component-playground__preview");
    public ILocator Widget => Preview.Locator(themeOnly ? ".rz-splitbutton" : route.EndsWith("/multi-button") ? ".admiral-multi-button" : ".admiral-menu-root").First;
    public ILocator Main => Widget.Locator("button").First;
    public ILocator Toggle => themeOnly ? Widget.Locator(":scope > button").Last : Widget.Locator(".admiral-menu__trigger button");
    public ILocator Popup => Page.Locator(".admiral-menu__popup:visible, .rz-splitbutton-menu:visible");
    public ILocator Result => Preview.Locator("output");
    public ILocator Controls => Page.GetByTestId("menu-action-playground").Locator(".admiral-component-controls");
    public async Task InitializeAsync()
    {
        await NavigateAsync(route);
        if (themeOnly)
        {
            await Page.Locator("[data-doc-tab=theme]").ClickAsync();
            await Widget.WaitForAsync();
        }
        await Toggle.WaitForAsync();
        if (!route.EndsWith("/tag-menu")) await Main.Locator("svg").First.WaitForAsync();
    }
    public Task SetAsync(string label, bool value) => Controls.GetByLabel(label, new() { Exact = true }).SetCheckedAsync(value);
    public Task SelectAsync(string label, string value) => Controls.GetByLabel(new Regex("^" + Regex.Escape(label))).SelectOptionAsync(value);
    public Task CloseOutsideAsync() => Page.Mouse.ClickAsync(1, 1);
    public Task<ButtonVisualState> ReadStyleAsync(ILocator element) => element.ReadSnapshotAsync<ButtonVisualState>(
        "e => { const s = getComputedStyle(e); return {color:s.color,background:s.backgroundColor,image:s.backgroundImage,border:s.borderColor,shadow:s.boxShadow,opacity:s.opacity}; }");
}
