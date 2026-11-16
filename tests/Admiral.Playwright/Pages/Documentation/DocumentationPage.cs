namespace Admiral.Playwright.Pages.Documentation;

internal sealed class DocumentationPage(IPage page) : GalleryPage(page)
{
    public ILocator Search => Page.GetByTestId("component-search");
    public ILocator ClearSearchButton => Page.GetByTestId("component-search-clear");
    public ILocator Sidebar => Page.Locator(".admiral-side-menu");

    public Task EnableClipboardAsync()
    {
        return Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
    }
}
