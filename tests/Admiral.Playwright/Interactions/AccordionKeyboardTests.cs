using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class AccordionKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Accordion: Enter/Space toggle; disabled header is inert / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Accordion: Enter/Space toggle; disabled header is inert / admiral-dark")]
    public async Task AccordionEnterSpaceToggleDisabledHeaderIsInertAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var header = Page.Locator(".rz-accordion-header button").First;

        // Act
        await header.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.rz-accordion-header button').getAttribute('aria-expanded') === 'false'");

        // Act
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.rz-accordion-header button').getAttribute('aria-expanded') === 'true'");

        await Assertions.Expect(Page.Locator(".admiral-accordion-item-disabled button")).ToBeDisabledAsync();
    }
}
