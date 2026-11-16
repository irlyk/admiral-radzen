namespace Admiral.Playwright.Pages.Components.Selection;

internal sealed class GlobalSearchPage(IPage page) : GalleryPage(page)
{
    public ILocator Global => Page.Locator(".admiral-global-search").First;
    public ILocator Input => Global.Locator("input");
    public ILocator Outcome => Page.Locator("[data-global-search-result]");

    public async Task PressKeyAsync(string value)
    {
        await Page.Keyboard.PressAsync(value);
        await Page.WaitForTimeoutAsync(70);
    }
}
