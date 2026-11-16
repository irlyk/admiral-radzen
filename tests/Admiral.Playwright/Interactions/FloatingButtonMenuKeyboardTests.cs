using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class FloatingButtonMenuKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: FloatingButtonMenu: hidden actions are inert; Escape returns to trigger / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: FloatingButtonMenu: hidden actions are inert; Escape returns to trigger / admiral-dark")]
    public async Task FloatingButtonMenuHiddenActionsAreInertEscapeReturnsToTriggerAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/floating-button-menu");
        var menu = Page.Locator(".admiral-floating-button-menu").First;
        var trigger = menu.Locator(":scope > .admiral-floating-button");

        // Assert
        Assert.That(
            await menu.Locator(".admiral-floating-button-menu__items").EvaluateAsync<bool>("e => e.inert"),
            Is.EqualTo(true));

        // Act
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-floating-button-menu').classList.contains('admiral-floating-button-menu--open')");
        await Page.WaitForTimeoutAsync(250);

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => !!document.activeElement.closest('.admiral-floating-button-menu__items')"),
            Is.True);

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('.admiral-floating-button-menu__items').inert");

        Assert.That(await trigger.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
    }
}
