using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class SwitchKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Switch: visible labels, native activation, disabled state and theme change / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Switch: visible labels, native activation, disabled state and theme change / admiral-dark")]
    public async Task SwitchVisibleLabelsNativeActivationDisabledStateAndThemeChangeAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/regression/overview");
        var section = Page.Locator("section[aria-labelledby=\"binary-heading\"]");
        var off = section.GetByRole(AriaRole.Switch, new LocatorGetByRoleOptions { Name = "Switch off", Exact = true });
        var on = section.GetByRole(AriaRole.Switch, new LocatorGetByRoleOptions { Name = "Switch on", Exact = true });
        var disabledOff = section.GetByRole(
            AriaRole.Switch,
            new LocatorGetByRoleOptions { Name = "Disabled off", Exact = true });
        var disabledOn = section.GetByRole(
            AriaRole.Switch,
            new LocatorGetByRoleOptions { Name = "Disabled on", Exact = true });
        await off.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        // Assert
        Assert.That(await on.GetAttributeAsync("aria-checked"), Is.EqualTo("true"));
        await Assertions.Expect(disabledOff).ToBeDisabledAsync();
        await Assertions.Expect(disabledOn).ToBeDisabledAsync();
        Assert.That(await disabledOff.GetAttributeAsync("aria-checked"), Is.EqualTo("false"));
        Assert.That(await disabledOn.GetAttributeAsync("aria-checked"), Is.EqualTo("true"));

        // Act
        await section.GetByText("Switch off", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

        // Assert
        await off.And(
            section.GetByRole(AriaRole.Switch, new LocatorGetByRoleOptions { Checked = true })).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        Assert.That(await off.EvaluateAsync<bool>("e => e === document.activeElement"), Is.True);

        // Act
        await off.PressAsync("Enter");

        // Assert
        await off.And(
            section.GetByRole(
                AriaRole.Switch,
                new LocatorGetByRoleOptions { Checked = false })).WaitForAsync(
                    new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        // Act
        await off.PressAsync("Space");

        // Assert
        await off.And(
            section.GetByRole(AriaRole.Switch, new LocatorGetByRoleOptions { Checked = true })).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        var visual = off.Locator("..");

        // Assert
        var width = (await visual.GetBoundsAsync()).Width;
        var height = (await visual.GetBoundsAsync()).Height;

        Assert.That(new[] { width, height }, Is.EqualTo(new[] { 36, 20 }));

        // Act
        await section.GetByText("Disabled off", new LocatorGetByTextOptions { Exact = true }).ClickAsync(new LocatorClickOptions { Force = true });

        // Assert
        Assert.That(await disabledOff.GetAttributeAsync("aria-checked"), Is.EqualTo("false"));

        Task<string> ReadBackgroundAsync() => visual.Locator(".rz-switch-circle").GetCssAsync("background-color");

        // Assert
        await visual.Locator(".rz-switch-circle").EvaluateAsync(
            "e => Promise.all(e.getAnimations().map(animation => animation.finished))");

        var before = await ReadBackgroundAsync();

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Switch Admiral color theme", Exact = true }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync("""
            (color) =>
              getComputedStyle(document.querySelector('#demo-switch-off + .rz-switch-circle'))
                .backgroundColor !== color
            """, before);

        Assert.That(await off.GetAttributeAsync("aria-checked"), Is.EqualTo("true"));

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Switch Admiral color theme", Exact = true }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync("""
            (color) =>
              getComputedStyle(document.querySelector('#demo-switch-off + .rz-switch-circle'))
                .backgroundColor === color
            """, before);
    }
}
