using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class RadioGroupKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Radio groups: accessible names, arrow selection, disabled skip and label click / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Radio groups: accessible names, arrow selection, disabled skip and label click / admiral-dark")]
    public async Task RadioGroupsAccessibleNamesArrowSelectionDisabledSkipAndLabelClickAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var group = Page.GetByRole(
            AriaRole.Radiogroup,
            new PageGetByRoleOptions { Name = "Radio button M", Exact = true });
        await Page.GetByRole(
            AriaRole.Radiogroup,
            new PageGetByRoleOptions { Name = "Radio button S", Exact = true }).WaitForAsync();
        var options = group.GetByRole(AriaRole.Radio);

        // Act
        await group.FocusAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "id => document.activeElement?.getAttribute('aria-activedescendant') === id",
            await options.Nth(0).GetAttributeAsync("id"));

        // Act
        await group.PressAsync("ArrowDown");

        // Assert
        await Page.WaitForFunctionAsync(
            "id => document.activeElement?.getAttribute('aria-activedescendant') === id",
            await options.Nth(1).GetAttributeAsync("id"));

        // Act
        await group.PressAsync("Space");

        var selected = group.GetByRole(AriaRole.Radio, new LocatorGetByRoleOptions { Checked = true });

        // Assert
        await options.Nth(1).And(selected).WaitForAsync();

        Assert.That(
            await options.Nth(1).Locator(".rz-radiobutton-box").GetCssAsync("outline-style"),
            Is.EqualTo("solid"));

        // Act
        await group.PressAsync("End");

        // Assert
        await options.Nth(2).And(selected).WaitForAsync();

        // Act
        await group.PressAsync("ArrowDown");

        // Assert
        await options.Nth(0).And(selected).WaitForAsync();

        // Act
        await options.Nth(1).Locator(".rz-radiobutton-label").ClickAsync();

        // Assert
        await options.Nth(1).And(selected).WaitForAsync();

        Assert.That(await options.Nth(3).GetAttributeAsync("aria-disabled"), Is.EqualTo("true"));
    }
}
