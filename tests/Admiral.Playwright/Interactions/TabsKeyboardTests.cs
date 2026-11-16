using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class TabsKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Tabs: ArrowRight and Enter change active panel / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Tabs: ArrowRight and Enter change active panel / admiral-dark")]
    public async Task TabsArrowRightAndEnterChangeActivePanelAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var tabs = Page.Locator(".admiral-tabs").First;

        // Act
        await tabs.GetByRole(AriaRole.Tablist).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.admiral-tabs [role=\"tab\"]')[1].getAttribute('aria-selected') === 'true'");
    }
}
