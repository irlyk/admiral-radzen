using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class TextAreaKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: TextArea: action hover is transparent, focus is square, Enter clears / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: TextArea: action hover is transparent, focus is square, Enter clears / admiral-dark")]
    public async Task TextAreaActionHoverIsTransparentFocusIsSquareEnterClearsAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/text-area");
        var field = Page.Locator(".admiral-component-playground .admiral-input-field");
        var label = field.Locator("label");
        var textarea = field.GetByRole(
            AriaRole.Textbox,
            new LocatorGetByRoleOptions { Name = await label.InnerTextAsync() });

        // Act
        await label.ClickAsync();

        // Assert
        Assert.That(
            await textarea.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true),
            "Label focuses the textarea");

        var action = field.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Clear value" });

        // Act
        await action.HoverAsync();

        // Assert
        Assert.That(await action.GetCssAsync("background-color"), Is.EqualTo("rgba(0, 0, 0, 0)"));

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await action.FocusAsync();

        // Assert
        Assert.That(await action.GetCssAsync("outline-style"), Is.Not.EqualTo("none"));
        Assert.That(CssPixels.Parse(await action.GetCssAsync("border-radius")) <= 4, Is.True);

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('textarea')?.value === ''");
    }

    [TestCase("admiral", TestName = "Interactions: TextArea: auto-height grows and shrinks after manual resize / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: TextArea: auto-height grows and shrinks after manual resize / admiral-dark")]
    public async Task TextAreaAutoHeightGrowsAndShrinksAfterManualResizeAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/text-area");
        var demo = Page.Locator(".admiral-component-playground");
        var textarea = demo.Locator("textarea");

        // Act
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "Resizable", Exact = true }).CheckAsync();

        // Assert
        await demo.Locator(".admiral-text-area-control--resizable").WaitForAsync();
        var bounds = await textarea.GetBoundsAsync();

        var x = bounds.X + bounds.Width - 4;
        var y = bounds.Y + bounds.Height - 4;

        // Act
        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)x, (float)(y + 40), new MouseMoveOptions { Steps = 6 });
        await Page.Mouse.UpAsync();

        // Assert
        Assert.That(
            (await textarea.GetBoundsAsync()).Height > bounds.Height,
            Is.True,
            "Native resize changed the height");

        // Act
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "AutoHeight", Exact = true }).CheckAsync();

        // Assert
        await demo.Locator(".admiral-text-area-control--auto").WaitForAsync();

        // Act
        await textarea.FillAsync("Short text");

        // Assert
        var shortHeight = (await textarea.GetBoundsAsync()).Height;

        // Act
        await textarea.FillAsync("One\nTwo\nThree\nFour");

        // Assert
        Assert.That((await textarea.GetBoundsAsync()).Height > shortHeight, Is.True, "Auto-height grows with content");

        // Act
        await textarea.FillAsync("Short text");

        // Assert
        Assert.That(
            (await textarea.GetBoundsAsync()).Height,
            Is.EqualTo(shortHeight),
            "Auto-height shrinks with content");
    }
}
