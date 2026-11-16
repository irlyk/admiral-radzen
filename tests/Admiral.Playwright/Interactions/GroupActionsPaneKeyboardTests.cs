using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class GroupActionsPaneKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: GroupActionsPane: opening search actually focuses input / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: GroupActionsPane: opening search actually focuses input / admiral-dark")]
    public async Task GroupActionsPaneOpeningSearchActuallyFocusesInputAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var pane = Page.Locator(".admiral-group-actions-pane").First;

        // Act
        await pane.Locator(".admiral-group-actions-pane__tools button").First.ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => document.activeElement?.closest('.admiral-group-actions-pane__search')");

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await pane.Locator("input").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }
}
