namespace Admiral.Playwright.Pages.Components.Hint;

internal sealed class HintPage(IPage page) : GalleryPage(page)
{
    public ILocator Opener => Page.GetByRole(
        AriaRole.Button,
        new PageGetByRoleOptions { Name = "Открыть Hint", Exact = true });
    public ILocator Popup => Page.Locator(".admiral-hint-popup");

    public async Task OpenAsync()
    {
        await Opener.ClickAsync();
        await Popup.WaitForAsync();
    }

    public async Task CloseAsync()
    {
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Закрыть Hint", Exact = true }).ClickAsync();
        await Popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    public async Task AssertInsideViewportAsync()
    {
        var box = await Popup.GetBoundsAsync();
        var viewport = new
        {
            width = Page.ViewportSize!.Width,
            height = Page.ViewportSize!.Height
        };
        Assert.That(box.X >= 15 && box.Y >= 15, Is.True, JsonSerializer.Serialize(box));
        Assert.That(box.X + box.Width <= viewport.width - 15, Is.True, JsonSerializer.Serialize(box));
        Assert.That(box.Y + box.Height <= viewport.height - 15, Is.True, JsonSerializer.Serialize(box));
    }

    public async Task InitializeAsync()
    {
        await Page.SetViewportSizeAsync(1440, 1000);
        await NavigateAsync("/components/hint");
        await Opener.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-hint-anchor button')?.getAttribute('aria-expanded') === 'false'");
    }
}
