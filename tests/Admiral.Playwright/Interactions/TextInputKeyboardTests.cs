using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class TextInputKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: TextInput: label focus, password reset and clear / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: TextInput: label focus, password reset and clear / admiral-dark")]
    public async Task TextInputLabelFocusPasswordResetAndClearAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/text-input");
        var demo = Page.Locator(".admiral-component-playground").First;
        var label = demo.Locator(".admiral-input-field__label");
        var input = demo.Locator(".admiral-text-input__control");
        var named = demo.GetByRole(
            AriaRole.Textbox,
            new LocatorGetByRoleOptions { Name = await label.InnerTextAsync(), Exact = true });

        // Act
        await label.ClickAsync();

        // Assert
        Assert.That(await named.EvaluateAsync<bool>("element => element === document.activeElement"), Is.True);

        var password = demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "Password", Exact = true });
        var show = demo.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Show password", Exact = true });
        var hide = demo.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Hide password", Exact = true });

        // Act
        await password.CheckAsync();

        // Assert
        await demo.Locator("input.admiral-text-input__control[type=\"password\"]").WaitForAsync();
        var original = await input.InputValueAsync();
        var beforeReveal = await demo.GetByRole(AriaRole.Status).InnerTextAsync();

        // Act
        await show.PressAsync("Enter");

        // Assert
        await hide.WaitForAsync();

        Assert.That(await input.GetAttributeAsync("type"), Is.EqualTo("text"));
        Assert.That(await input.InputValueAsync(), Is.EqualTo(original));
        Assert.That(await demo.GetByRole(AriaRole.Status).InnerTextAsync(), Is.EqualTo(beforeReveal));

        // Act
        await password.UncheckAsync();

        // Assert
        await hide.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Act
        await password.CheckAsync();

        // Assert
        await show.WaitForAsync();

        Assert.That(await input.GetAttributeAsync("type"), Is.EqualTo("password"));

        var readOnly = demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "Read only", Exact = true });

        // Act
        await readOnly.CheckAsync();

        // Assert
        await demo.Locator("input.admiral-text-input__control[readonly]").WaitForAsync();

        await Assertions.Expect(show).ToBeDisabledAsync();
        await Assertions.Expect(
            demo.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Clear value", Exact = true })).ToHaveCountAsync(0);

        // Act
        await readOnly.UncheckAsync();

        var clear = demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Clear value", Exact = true });

        // Act
        await clear.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-component-playground .admiral-text-input__control')?.value === ''");

        var disabled = demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "Disabled", Exact = true });

        // Act
        await disabled.CheckAsync();

        // Assert
        await demo.Locator("input.admiral-text-input__control[disabled]").WaitForAsync();

        await Assertions.Expect(show).ToBeDisabledAsync();

        // Act
        await disabled.UncheckAsync();
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "Loading", Exact = true }).CheckAsync();

        // Assert
        await demo.Locator(".admiral-text-input[aria-busy=\"true\"]").WaitForAsync();

        await Assertions.Expect(input).ToBeDisabledAsync();
        await show.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }

    [TestCase("admiral", TestName = "Interactions: Direct RadzenTextBox: typing, blur and disabled / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Direct RadzenTextBox: typing, blur and disabled / admiral-dark")]
    public async Task DirectRadzenTextBoxTypingBlurAndDisabledAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/text-input");

        // Act
        await Page.Locator("[data-doc-tab=\"theme\"]").ClickAsync();

        var demo = Page.Locator(".admiral-component-playground").First;
        var input = demo.Locator(".admiral-component-playground__preview .rz-textbox");

        // Act
        await input.ClickAsync();
        await input.FillAsync("Direct Radzen text");
        await input.PressAsync("Tab");
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "Disabled", Exact = true }).CheckAsync();

        // Assert
        await demo.Locator(".admiral-component-playground__preview .rz-textbox[disabled]").WaitForAsync();

        Assert.That(await input.InputValueAsync(), Is.EqualTo("Direct Radzen text"));
    }
}
