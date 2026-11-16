using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class NumberInputKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: NumberInput: label, literal text, steppers, bounds and clear / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: NumberInput: label, literal text, steppers, bounds and clear / admiral-dark")]
    public async Task NumberInputLabelLiteralTextSteppersBoundsAndClearAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/number-input");
        var demo = Page.Locator(".admiral-component-playground").First;
        var field = demo.GetByRole(AriaRole.Spinbutton, new LocatorGetByRoleOptions { Name = "Amount" });

        // Act
        await demo.Locator(".admiral-input-field__label").ClickAsync();

        // Assert
        Assert.That(
            await field.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.True,
            "Label focuses the spinbutton");

        var status = demo.GetByRole(AriaRole.Combobox, new LocatorGetByRoleOptions { Name = "Status", Exact = true });

        // Act
        await status.SelectOptionAsync("Error");

        // Assert
        await Page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('.admiral-component-playground [role="spinbutton"]')
                ?.getAttribute('aria-invalid') === 'true'
            """);

        // Act
        await status.SelectOptionAsync("Success");

        // Assert
        await Page.WaitForFunctionAsync("""
            () =>
              !document
                .querySelector('.admiral-component-playground [role="spinbutton"]')
                ?.hasAttribute('aria-invalid')
            """);

        var prefix = demo.Locator(
            ".admiral-component-controls input[type=\"text\"], .admiral-component-controls input:not([type])").First;

        // Act
        await prefix.FillAsync("Owner's ");
        await prefix.PressAsync("Tab");

        // Assert
        await Page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('.admiral-component-playground [role="spinbutton"]')
                ?.value.startsWith("Owner's ")
            """);

        // Act
        await field.FillAsync("Owner's 5,050.50 ₽");
        await field.PressAsync("Tab");

        // Assert
        await demo.GetByRole(AriaRole.Status).GetByText(new Regex("5050.50")).WaitForAsync();

        // Act
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Increase value" }).ClickAsync();

        // Assert
        await demo.GetByRole(AriaRole.Status).GetByText(new Regex("5051.50")).WaitForAsync();

        // Act
        await demo.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Clear value" }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-component-playground [role=\"spinbutton\"]')?.value === ''");

        var rate = Page.GetByRole(AriaRole.Spinbutton, new PageGetByRoleOptions { Name = "Rate", Exact = true });

        // Act
        await rate.FocusAsync();
        await rate.PressAsync("ArrowUp");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('input[aria-valuemax=\"100\"]')?.value === '12.75 %'");

        // Act
        await rate.FillAsync("150 %");
        await rate.PressAsync("Tab");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('input[aria-valuemax=\"100\"]')?.value === '100.00 %'");

        // Act
        await rate.FillAsync("-10 %");
        await rate.PressAsync("Tab");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('input[aria-valuemax=\"100\"]')?.value === '0.00 %'");
    }

    [TestCase("admiral", TestName = "Interactions: Direct RadzenNumeric: binding, step and upper bound / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Direct RadzenNumeric: binding, step and upper bound / admiral-dark")]
    public async Task DirectRadzenNumericBindingStepAndUpperBoundAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/number-input");

        // Act
        await Page.Locator("[data-doc-tab=\"theme\"]").ClickAsync();

        var demo = Page.Locator(".admiral-component-playground").First;
        var field = demo.Locator(".admiral-component-playground__preview").GetByRole(AriaRole.Spinbutton);
        var bound = demo.Locator("input[type=\"number\"]");

        // Assert
        await bound.WaitForAsync();

        // Act
        await field.FillAsync("999");
        await field.PressAsync("Tab");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-component-playground input[type=\"number\"]')?.value === '999'");

        // Act
        await demo.Locator(".rz-numeric-up").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-component-playground input[type=\"number\"]')?.value === '1000'");

        // Act
        await demo.Locator(".rz-numeric-up").ClickAsync();

        // Assert
        Assert.That(await bound.InputValueAsync(), Is.EqualTo("1000"));

        // Act
        await demo.Locator(".rz-numeric-down").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-component-playground input[type=\"number\"]')?.value === '999'");
    }
}
