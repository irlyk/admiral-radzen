using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class MenuKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Menu: arrows, Home/End, checkbox, Escape, return focus, disabled skip / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Menu: arrows, Home/End, checkbox, Escape, return focus, disabled skip / admiral-dark")]
    public async Task MenuArrowsHomeEndCheckboxEscapeReturnFocusDisabledSkipAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/menu");
        var trigger = Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Open item examples", Exact = true });

        // Act: open the menu using the keyboard.
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await Page.WaitForSelectorAsync(".admiral-menu__popup");
        await Page.WaitForFunctionAsync("() => document.activeElement?.classList.contains('admiral-menu-item')");

        Assert.That(await gallery.GetActiveTextAsync(), Does.Match(new Regex("Edit payment")));

        // Act: move to the next enabled item.
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        Assert.That(await gallery.GetActiveTextAsync(), Does.Match(new Regex("Pin in recent")));

        // Act: toggle the checkable menu item.
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync("() => document.activeElement?.getAttribute('aria-checked') === 'true'");

        // Act: move to the last enabled item.
        await Page.Keyboard.PressAsync("End");

        // Assert
        Assert.That(await gallery.GetActiveTextAsync(), Does.Match(new Regex("Delete")));

        // Act: move back over the disabled entry.
        await Page.Keyboard.PressAsync("ArrowUp");

        // Assert
        Assert.That(await gallery.GetActiveTextAsync(), Does.Match(new Regex("Current operation")));

        // Act: return to the first enabled item.
        await Page.Keyboard.PressAsync("Home");

        // Assert
        Assert.That(await gallery.GetActiveTextAsync(), Does.Match(new Regex("Edit payment")));

        // Act: dismiss the menu and return focus to its trigger.
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Page.WaitForSelectorAsync(
            ".admiral-menu__popup",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });

        Assert.That(await trigger.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        Assert.That(await trigger.GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));
    }

    [TestCase("admiral", TestName = "Interactions: Menu: Tab exits; resize keeps popup in viewport / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Menu: Tab exits; resize keeps popup in viewport / admiral-dark")]
    public async Task MenuTabExitsResizeKeepsPopupInViewportAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/menu");
        var trigger = Page.Locator(".admiral-layout__body").GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Actions", Exact = true }).First;

        // Act: open the popup.
        await trigger.ClickAsync();

        // Assert
        await Page.WaitForSelectorAsync(".admiral-menu__popup");

        // Act: shrink the browser while the popup is open.
        await Page.SetViewportSizeAsync(600, 900);
        await Page.WaitForTimeoutAsync(250);

        // Assert
        var bounds = await Page.Locator(".admiral-menu__popup").GetBoundsAsync();

        Assert.That(bounds.X >= 0 && bounds.X + bounds.Width <= 600, Is.True);

        // Act: leave the popup using Tab.
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Page.WaitForSelectorAsync(
            ".admiral-menu__popup",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });

        // Cleanup: restore the original viewport.
        await Page.SetViewportSizeAsync(1440, 1000);
    }
}
