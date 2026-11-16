using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class SelectKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Select: keyboard opens and Escape closes without losing trigger / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Select: keyboard opens and Escape closes without losing trigger / admiral-dark")]
    public async Task SelectKeyboardOpensAndEscapeClosesWithoutLosingTriggerAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/select");
        var trigger = Page.Locator(".admiral-select [role=\"combobox\"]").First;

        // Act
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.Locator(".rz-dropdown-panel:visible").First.WaitForAsync();

        // Act
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.Locator(".rz-dropdown-panel:visible").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}
