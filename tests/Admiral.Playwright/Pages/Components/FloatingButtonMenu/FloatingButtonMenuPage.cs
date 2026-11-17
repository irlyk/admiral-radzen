namespace Admiral.Playwright.Pages.Components.FloatingButtonMenu;

internal sealed class FloatingButtonMenuPage(IPage page) : GalleryPage(page)
{
    public ILocator Preview => Page.Locator(".admiral-component-playground__preview").First;
    public ILocator Menu => Preview.Locator(".admiral-floating-button-menu, .rz-fabmenu");
    public ILocator Trigger => Menu.Locator(":scope > button");
    public ILocator Items => Menu.Locator(".admiral-floating-button-menu__items, .rz-fabmenu-items");
    public ILocator Actions => Items.Locator("button");
    public ILocator UnboundTrigger => Page.Locator(".admiral-floating-button-menu").Last.Locator(":scope > button");
    public ILocator Result => Preview.Locator("[aria-live=polite]");

    public async Task InitializeAsync(bool themeOnly = false)
    {
        await NavigateAsync("/components/floating-button-menu");
        if (themeOnly)
        {
            await Page.Locator("[data-doc-tab=theme]").ClickAsync();
            await Assertions.Expect(Menu).ToHaveClassAsync(new Regex("(^| )rz-fabmenu( |$)"));
        }
        await Trigger.Locator("svg").WaitForAsync();
    }

    public Task SetAsync(string name, bool value) => Page.GetByLabel(name, new() { Exact = true }).SetCheckedAsync(value);
    public Task SelectSizeAsync(string size) => Page.GetByLabel("Size", new() { Exact = false }).SelectOptionAsync(size);
    public Task CloseOutsideAsync(bool themeOnly = false) => themeOnly
        ? Page.Mouse.ClickAsync(1, 1)
        : Page.Locator(".admiral-floating-button-menu__backdrop").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });
}
