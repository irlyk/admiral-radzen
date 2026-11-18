namespace Admiral.Playwright.Pages.Components.FloatingButton;

internal sealed class FloatingButtonPage(IPage page) : GalleryPage(page)
{
    public ILocator Preview => Page.Locator(".admiral-component-playground__preview").First;
    public ILocator Button => Preview.Locator("button.admiral-floating-button");
    public ILocator Content => Button.Locator(".admiral-floating-button__content");
    public ILocator Controls => Page.Locator(".admiral-component-controls").First;
    public ILocator Result => Preview.Locator("output");
    public async Task InitializeAsync(bool themeOnly)
    {
        await NavigateAsync("/components/floating-button");
        if (themeOnly)
        {
            var tab = Page.Locator("[data-doc-tab=theme]");
            await tab.ClickAsync();
            await Assertions.Expect(tab).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(Button).ToHaveClassAsync(new Regex("(^| )rz-button( |$)"));
        }
        await Button.Locator("svg").WaitForAsync();
    }
    public Task SetAsync(string name, bool value) => Controls.GetByLabel(name, new() { Exact = true }).SetCheckedAsync(value);
    public Task SelectAsync(string name, string value) => Controls.GetByLabel(new Regex("^" + name)).SelectOptionAsync(value);
}
