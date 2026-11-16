namespace Admiral.Playwright.Pages.Components.Drawer;

internal sealed class DrawerPage(IPage page) : GalleryPage(page)
{
    public ILocator Drawer => Page.Locator(".admiral-drawer");
    public ILocator Backdrop => Page.Locator(".admiral-drawer-backdrop");
    public ILocator Opener => Page.GetByRole(
        AriaRole.Button,
        new PageGetByRoleOptions { Name = "Открыть Drawer", Exact = true });

    public async Task OpenAsync()
    {
        await Opener.ClickAsync();
        await Drawer.WaitForAsync();
        await Page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('.admiral-drawer');
              return root && root.getAnimations().every((animation) => animation.playState === 'finished');
            }
            """);
    }

    public async Task CloseAsync()
    {
        await Drawer.Locator(".admiral-drawer__close").ClickAsync();
        await Drawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    public async Task InitializeAsync()
    {
        await Page.SetViewportSizeAsync(1440, 1000);
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await NavigateAsync("/components/drawer");
        await Opener.WaitForAsync();
    }
}
