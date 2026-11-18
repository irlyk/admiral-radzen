namespace Admiral.Playwright.Pages.Components.Layout;

internal sealed class LayoutPage(IPage page, bool themeOnly = false) : GalleryPage(page)
{
    public ILocator Demo => Page.GetByTestId("layout-playground");
    public ILocator Layout => Demo.GetByTestId("layout-sample");
    public ILocator Header => Layout.Locator(":scope > .rz-header");
    public ILocator Sidebar => Layout.Locator(":scope > .rz-sidebar");
    public ILocator Body => Layout.Locator(":scope > .rz-body");
    public ILocator Footer => Layout.Locator(":scope > .rz-footer");
    public ILocator Controls => Demo.Locator(".admiral-component-controls");
    public async Task InitializeAsync(string composition)
    {
        await NavigateAsync("/components/layout");
        if (themeOnly)
        {
            await Page.Locator("[data-doc-tab=theme]").ClickAsync();
            await Assertions.Expect(Layout).Not.ToHaveClassAsync(new Regex("admiral-layout"));
        }
        await Controls.GetByLabel("Composition", new() { Exact = true }).SelectOptionAsync(composition);
        await Assertions.Expect(Demo.Locator("output")).ToContainTextAsync(composition);
        await Layout.ScrollIntoViewIfNeededAsync();
    }
    public async Task SetLengthAsync(string label, string value)
    {
        var input = Controls.GetByLabel(label, new() { Exact = true });
        await input.FillAsync(value);
        await input.PressAsync("Tab");
        await Layout.ScrollIntoViewIfNeededAsync();
    }
    public Task ToggleSidebarAsync() => Controls.GetByRole(AriaRole.Button, new() { Name = "Toggle sidebar", Exact = true }).ClickAsync();
    public async Task SetTallAsync(bool value)
    {
        await Controls.GetByLabel("Tall content", new() { Exact = true }).SetCheckedAsync(value);
        await Assertions.Expect(Body.GetByText("Scrollable content 34", new() { Exact = true })).ToHaveCountAsync(value ? 1 : 0);
    }
    public Task<LayoutGeometry> MeasureAsync() => Layout.ReadSnapshotAsync<LayoutGeometry>("""
        e => {
          const rect = selector => e.querySelector(selector)?.getBoundingClientRect().toJSON() ?? null;
          const body = e.querySelector(':scope > .rz-body');
          return {root:e.getBoundingClientRect().toJSON(),header:rect(':scope > .rz-header'),sidebar:rect(':scope > .rz-sidebar'),
            body:body.getBoundingClientRect().toJSON(),footer:rect(':scope > .rz-footer'),scrollHeight:body.scrollHeight,clientHeight:body.clientHeight};
        }
        """);
}
