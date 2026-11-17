namespace Admiral.Playwright.Pages.Components.Button;

internal sealed class ButtonPage(IPage page) : GalleryPage(page)
{
    public ILocator Preview => Page.Locator(".admiral-component-playground__preview").First;
    public ILocator Button => Preview.Locator("button").First;
    public ILocator Result => Preview.Locator(":scope > [aria-live], :scope > [role=status]").First;
    public ILocator Controls => Page.Locator(".admiral-component-controls").First;

    public async Task InitializeAsync(string route, bool themeOnly)
    {
        await NavigateAsync(route);
        if (themeOnly)
        {
            await Page.Locator("[data-doc-tab=theme]").ClickAsync();
        }
        await Button.WaitForAsync();
        var icons = Button.Locator(".admiral-icon, .rz-button-icon-left");
        if (await icons.CountAsync() > 0)
        {
            await icons.First.Locator("svg").WaitForAsync();
        }
    }

    public Task SetStateAsync(string name, bool value) => Controls.GetByLabel(name, new() { Exact = true }).SetCheckedAsync(value);
    public Task SetOptionAsync(string name, string value) => Controls.GetByLabel(new Regex("^" + Regex.Escape(name))).SelectOptionAsync(value);

    public Task<ButtonVisualState> ReadStateAsync() => Button.ReadSnapshotAsync<ButtonVisualState>(
        """
        e => {
          const s = getComputedStyle(e);
          return { color: s.color, background: s.backgroundColor, image: s.backgroundImage,
            border: s.borderColor, shadow: s.boxShadow, opacity: s.opacity };
        }
        """);
}
