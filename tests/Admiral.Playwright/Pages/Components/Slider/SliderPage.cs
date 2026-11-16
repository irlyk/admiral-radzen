namespace Admiral.Playwright.Pages.Components.Slider;

internal sealed class SliderPage(IPage page) : GalleryPage(page)
{
    public ILocator Slider => Page.Locator(".admiral-slider").First;
    public ILocator Handles => Slider.GetByRole(AriaRole.Slider);

    public async Task ExpectValueAsync(ILocator handle, double value)
    {
        await Page.WaitForFunctionAsync(
            "([name, value]) => Number(document.querySelector(`[role=slider][aria-label=\"${name}\"]`)?.getAttribute('aria-valuenow')) === value",
            new object?[] { await handle.GetAttributeAsync("aria-label"), value });
    }

    public async Task InitializeAsync()
    {
        await Page.SetViewportSizeAsync(1440, 1000);
        await NavigateAsync("/components/slider");
        await Handles.First.WaitForAsync();
    }
}
