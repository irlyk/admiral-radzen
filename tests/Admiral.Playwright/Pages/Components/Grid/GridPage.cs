namespace Admiral.Playwright.Pages.Components.Grid;

internal sealed class GridPage(IPage page, bool native = false) : GalleryPage(page)
{
    public ILocator Demo => Page.GetByTestId(native ? "grid-radzen-demo" : "grid-admiral-demo");
    public ILocator Grid => Demo.Locator(native ? ".rz-row" : ".admiral-grid");
    public ILocator Items => Demo.Locator(".grid-playground__tile");
    public async Task InitializeAsync()
    {
        await NavigateAsync("/components/grid");
        if (native) await Page.Locator("[data-doc-tab=theme]").ClickAsync();
        await Assertions.Expect(Items).ToHaveCountAsync(6);
    }
    public async Task SetAsync(string label, string value)
    {
        var input = Demo.GetByLabel(label, new() { Exact = true });
        await input.FillAsync(value);
        await input.PressAsync("Tab");
    }
    public Task SetAlternativeAsync(bool value) => Demo.GetByLabel("Alternative", new() { Exact = true }).SetCheckedAsync(value);
    public Task<GridGeometry> MeasureAsync() => Grid.ReadSnapshotAsync<GridGeometry>("""
        e => {
          const s = getComputedStyle(e), padding = parseFloat(s.paddingLeft);
          return { root:e.getBoundingClientRect().toJSON(),contentWidth:e.clientWidth-padding-parseFloat(s.paddingRight),
            gap:parseFloat(s.columnGap),padding,columns:s.display==='grid'?s.gridTemplateColumns.trim().split(/\s+/).length:12,
            items:[...e.querySelectorAll('.grid-playground__tile')].map(n=>n.getBoundingClientRect().toJSON())};
        }
        """);
}
