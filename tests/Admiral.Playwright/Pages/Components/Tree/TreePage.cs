namespace Admiral.Playwright.Pages.Components.Tree;

internal sealed class TreePage(IPage page) : GalleryPage(page)
{
    public ILocator Tree => Page.GetByRole(
        AriaRole.Tree,
        new PageGetByRoleOptions { Name = "Документы компании", Exact = true });

    public ILocator GetRow(string id) => Tree.Locator($"[data-tree-id=\"{id}\"]");

    public Task WaitForRowAttributeAsync(string id, string attribute, string value) =>
        Assertions.Expect(GetRow(id)).ToHaveAttributeAsync(attribute, value);

    public Task WaitForFocusAsync(string id) => Assertions.Expect(GetRow(id)).ToBeFocusedAsync();

    public async Task InitializeAsync()
    {
        await Page.SetViewportSizeAsync(1440, 1000);
        await NavigateAsync("/components/tree");
        await Tree.WaitForAsync();
    }
}
