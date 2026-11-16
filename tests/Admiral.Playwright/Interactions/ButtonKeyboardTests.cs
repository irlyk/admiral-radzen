using Admiral.Playwright.Pages;

namespace Admiral.Playwright.Interactions;

[Category("Interactions")]
public sealed class ButtonKeyboardTests : GalleryTest
{
    [TestCase("admiral", TestName = "Interactions: Button: Enter and Space invoke the public callback / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Button: Enter and Space invoke the public callback / admiral-dark")]
    public async Task ButtonEnterAndSpaceInvokeThePublicCallbackAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/button");
        var preview = Page.Locator(".admiral-component-playground__preview").First;

        // Act
        await preview.Locator("button").First.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await preview.GetByText("Click count: 1", new LocatorGetByTextOptions { Exact = true }).WaitForAsync();

        // Act
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await preview.GetByText("Click count: 2", new LocatorGetByTextOptions { Exact = true }).WaitForAsync();
    }

    [TestCase("admiral", TestName = "Interactions: Button: blocked submit/reset are inert and square button has a name / admiral")]
    [TestCase("admiral-dark", TestName = "Interactions: Button: blocked submit/reset are inert and square button has a name / admiral-dark")]
    public async Task ButtonBlockedSubmitResetAreInertAndSquareButtonHasANameAsync(string theme)
    {
        // Arrange
        await StartPageAsync(theme, "en");
        var gallery = new GalleryPage(Page);
        await gallery.NavigateAsync("/components/button");
        var form = Page.Locator(".admiral-button-form-regression");
        var value = form.GetByRole(AriaRole.Textbox, new LocatorGetByRoleOptions { Name = "Form value" });
        var count = form.GetByText("Form submit count: 0", new LocatorGetByTextOptions { Exact = true });
        var blocked = new[]
        {
            "Loading",
            "Skeleton",
            "Disabled"
        };

        // Act & assert
        foreach (var state in blocked)
        {
            // Act & assert
            foreach (var type in new[]
            {
                "submit",
                "reset"
            })
            {
                // Assert
                var button = form.Locator("button").Filter(new LocatorFilterOptions { HasText = $"{state} {type}" });

                Assert.That(
                    await button.IsDisabledAsync(),
                    Is.EqualTo(true),
                    $"{state} {type} must use native disabled");

                // Act
                if (type == "reset")
                {
                    await value.FillAsync("Changed");
                }

                // Assert
                await button.EvaluateAsync("e => e.click()");
                await count.WaitForAsync();
                if (type == "reset")
                {
                    Assert.That(
                        await value.InputValueAsync(),
                        Is.EqualTo("Changed"),
                        $"{state} reset must not reset the form");
                }
            }
        }

        // Act
        await value.FillAsync("Changed");
        await form.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Active reset" }).ClickAsync();

        // Assert
        Assert.That(await value.InputValueAsync(), Is.EqualTo("Original"));

        // Act
        await form.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Active submit" }).ClickAsync();

        // Assert
        await form.GetByText("Form submit count: 1", new LocatorGetByTextOptions { Exact = true }).WaitForAsync();

        var square = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Square action", Exact = true });

        // Assert
        await square.WaitForAsync();

        Assert.That(await square.GetAttributeAsync("aria-label"), Is.EqualTo("Square action"));
    }
}
