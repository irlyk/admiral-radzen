namespace Admiral.Playwright.Pages.Components.ButtonGroup;

internal sealed class ButtonGroupPage(IPage page) : GalleryPage(page)
{
    public ILocator Preview => Page.Locator(".admiral-component-playground__preview").First;
    public ILocator Group => Preview.GetByRole(AriaRole.Group, new() { Name = "Document actions", Exact = true });
    public ILocator Buttons => Group.Locator(":scope > button");
    public ILocator Result => Preview.Locator(":scope > output");
    public ILocator Controls => Page.Locator(".admiral-component-controls").First;

    public async Task InitializeAsync(string route, bool themeOnly)
    {
        await NavigateAsync(route);
        if (themeOnly)
        {
            await Page.Locator("[data-doc-tab=theme]").ClickAsync();
        }
        await Group.WaitForAsync();
        await Assertions.Expect(Buttons).ToHaveCountAsync(3);
        var icons = Group.Locator(".admiral-icon, .rz-button-icon-left");
        if (await icons.CountAsync() > 0)
        {
            await Assertions.Expect(icons.Locator("svg")).ToHaveCountAsync(3);
        }
    }

    public Task SetStateAsync(string name, bool value) => Controls.GetByLabel(name, new() { Exact = true }).SetCheckedAsync(value);
    public Task SetOptionAsync(string name, string value) => Controls.GetByLabel(new Regex("^" + Regex.Escape(name))).SelectOptionAsync(value);
    public Task<ButtonVisualState> ReadStateAsync(int index) => Buttons.Nth(index).ReadSnapshotAsync<ButtonVisualState>(
        """
        e => {
          const s = getComputedStyle(e);
          return { color: s.color, background: s.backgroundColor, image: s.backgroundImage,
            border: s.borderColor, shadow: s.boxShadow, opacity: s.opacity };
        }
        """);

    public Task<ButtonGroupGeometry> MeasureAsync() => Group.ReadSnapshotAsync<ButtonGroupGeometry>(
        """
        e => ({ group: e.getBoundingClientRect().toJSON(), buttons: Array.from(e.querySelectorAll(':scope > button')).map(b => {
          const s = getComputedStyle(b), content = b.querySelector('.rz-button-box');
          return { bounds: b.getBoundingClientRect().toJSON(), startStart: parseFloat(s.borderStartStartRadius),
            startEnd: parseFloat(s.borderStartEndRadius), endStart: parseFloat(s.borderEndStartRadius),
            endEnd: parseFloat(s.borderEndEndRadius), content: content.getBoundingClientRect().toJSON() };
        }) })
        """);
}
