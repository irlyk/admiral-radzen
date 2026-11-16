namespace Admiral.Playwright.Pages.Components.Selection;

internal sealed class TreeSelectPage(IPage page) : GalleryPage(page)
{
    public ILocator Tree => Page.Locator(".admiral-tree-select");
    public ILocator Trigger => Tree.Locator(".admiral-tree-select__trigger");
    public ILocator Search => Tree.Locator(".admiral-tree-select__search");

    public ILocator GetNode(string name) => Tree.GetByRole(
        AriaRole.Treeitem,
        new LocatorGetByRoleOptions { Name = name, Exact = true });

    public Task<IJSHandle> WaitForActiveNodeAsync(string name) => Page.WaitForFunctionAsync("""
        (name) => {
          const control = document.querySelector('.admiral-tree-select__trigger');
          return (
            document
              .getElementById(control?.getAttribute('aria-activedescendant'))
              ?.getAttribute('aria-label') === name
          );
        }
        """, name);

    public Task<IJSHandle> WaitForNodeCheckedAsync(string name, string value) => Page.WaitForFunctionAsync(
        """
        ({ name, value }) =>
          [...document.querySelectorAll('.admiral-tree-select [role="treeitem"]')].some(
            (e) => e.getAttribute('aria-label') === name && e.getAttribute('aria-checked') === value,
          )
        """,
        new { name = name, value = value });

    public async Task PressKeyAsync(string value)
    {
        await Page.Keyboard.PressAsync(value);
        await Page.WaitForTimeoutAsync(70);
    }
}
