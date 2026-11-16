using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class BinaryControlsKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Checkbox and Switch: Space toggles; disabled controls leave tab order / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Checkbox and Switch: Space toggles; disabled controls leave tab order / admiral-dark")]
    public async Task CheckboxAndSwitchSpaceTogglesDisabledControlsLeaveTabOrderAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var section = Page.Locator("section[aria-labelledby=\"binary-heading\"]");

        // Act & assert
        foreach (var role in new[]
        {
            "checkbox",
            "switch"
        })
        {
            var target = section.GetByRole(Enum.Parse<AriaRole>(role, true)).First;

            // Act
            await target.FocusAsync();
            await Page.Keyboard.PressAsync("Space");

            // Assert
            await Page.WaitForFunctionAsync(
                "role => document.querySelector(`section[aria-labelledby=\"binary-heading\"] [role=\"${role}\"]`).getAttribute('aria-checked') === 'true'",
                role);
            var disabled = section.Locator($"[role=\"{role}\"][tabindex=\"-1\"]").First;

            Assert.That(await disabled.CountAsync(), Is.Not.Zero);
        }
    }
}
