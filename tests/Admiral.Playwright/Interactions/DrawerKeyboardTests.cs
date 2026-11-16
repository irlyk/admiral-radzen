using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class DrawerKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Drawer: Shift+Tab/Tab trap, Escape, return focus / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Drawer: Shift+Tab/Tab trap, Escape, return focus / admiral-dark")]
    public async Task DrawerShiftTabTabTrapEscapeReturnFocusAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var opener = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open drawer", Exact = true });

        // Act
        await opener.ClickAsync();

        // Assert
        await Page.WaitForSelectorAsync(".admiral-drawer");
        await Page.WaitForFunctionAsync("() => document.activeElement?.closest('.admiral-drawer')");
        var initial = await Page.EvaluateAsync<string>("() => document.activeElement.getAttribute('aria-label')");

        // Act
        await Page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => !!document.activeElement.closest('.admiral-drawer__actions')"),
            Is.True);

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(
            await Page.EvaluateAsync<string>("() => document.activeElement.getAttribute('aria-label')"),
            Is.EqualTo(initial));

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.WaitForSelectorAsync(
            ".admiral-drawer",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });

        Assert.That(await opener.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
    }
}
