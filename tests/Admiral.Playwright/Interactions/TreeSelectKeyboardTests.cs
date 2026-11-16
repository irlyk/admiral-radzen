using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class TreeSelectKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: TreeSelect: Escape from popup restores trigger / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: TreeSelect: Escape from popup restores trigger / admiral-dark")]
    public async Task TreeSelectEscapeFromPopupRestoresTriggerAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/tree-select");
        var trigger = Page.Locator(".admiral-tree-select__trigger").First;

        // Act
        await trigger.ClickAsync();

        // Assert
        await Page.Locator(".admiral-tree-select__popup").WaitForAsync();

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForFunctionAsync(
            "() => document.activeElement?.classList.contains('admiral-tree-select__trigger')");
    }
}
