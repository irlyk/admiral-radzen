namespace Admiral.Playwright.Pages.Components.ActionBar;

internal sealed class ActionBarPage(IPage page) : GalleryPage(page)
{
    public ILocator Toolbar =>
        Page.GetByRole(
            AriaRole.Toolbar,
            new PageGetByRoleOptions { Name = "Действия с документом", Exact = true });
    public ILocator Actions => Toolbar.Locator(":scope > button");
    public ILocator OverflowButton =>
        Toolbar.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Ещё действия", Exact = true });

    public Task<IReadOnlyList<string>> GetMenuTextsAsync() =>
        Page.Locator("[role=\"menuitem\"] .admiral-menu-item__text").AllTextContentsAsync();

    public async Task WaitForMenuFocusAsync()
    {
        await Page.GetByRole(AriaRole.Menu).WaitForAsync();
        await Page.WaitForFunctionAsync("() => document.activeElement?.closest('[role=\"menu\"]')");
    }

    public async Task InitializeAsync()
    {
        await Page.SetViewportSizeAsync(1440, 1000);
        await NavigateAsync("/components/action-bar");
        await Toolbar.WaitForAsync();
    }

    public async Task SetWidthAsync(int value)
    {
        await Page.Locator(".action-bar-demo-host").EvaluateAsync("(element, value) => element.style.width = value + 'px'", value);
    }

    public async Task ExpectVisibleActionCountAsync(int value)
    {
        await Assertions.Expect(Page.Locator(".admiral-action-bar--adaptive > button")).ToHaveCountAsync(value);
    }
}
