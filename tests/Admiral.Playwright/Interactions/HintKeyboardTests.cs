using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class HintKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Hint: keyboard focus stays inside; Escape returns to trigger / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Hint: keyboard focus stays inside; Escape returns to trigger / admiral-dark")]
    public async Task HintKeyboardFocusStaysInsideEscapeReturnsToTriggerAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var trigger = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Large hint", Exact = true });

        // Act
        await trigger.ClickAsync();

        // Assert
        await Page.Locator(".admiral-hint__dialog").WaitForAsync();
        await Page.WaitForFunctionAsync("() => document.activeElement?.closest('.admiral-hint__dialog')");

        // Act & assert
        for (var i = 0; i < 5; i++)
        {
            // Act
            await Page.Keyboard.PressAsync("Tab");

            // Assert
            Assert.That(
                await Page.EvaluateAsync<bool>(
                    "() => !!document.activeElement.closest('.admiral-hint__dialog')"),
                Is.True);
        }

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.Locator(".admiral-hint__dialog").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForFunctionAsync("() => document.activeElement?.textContent.includes('Large hint')");
    }
}
