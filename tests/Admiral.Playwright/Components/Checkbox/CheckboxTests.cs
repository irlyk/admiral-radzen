namespace Admiral.Playwright.Components.Checkbox;

[Category("Checkbox")]
public sealed class CheckboxTests : BrowserTest
{
    [TestCase("admiral", "ru", 1280, 900)]
    [TestCase("admiral", "en", 1280, 900)]
    [TestCase("admiral-dark", "ru", 1280, 900)]
    [TestCase("admiral-dark", "en", 1280, 900)]
    public async Task CheckboxStatesAndCompositeGroupAsync(string theme, string culture, int width, int height)
    {
        // Arrange
        var language = culture;
        var viewport = new
        {
            width,
            height
        };
        var (context, page) = await NewPageAsync(theme, culture, width, height);
        var baseUrl = BaseUrl;
        var output = Output;
        var label = (language == "ru") ? "Флажок" : "Checkbox";
        var controls = (language == "ru")
            ? new
            {
                disabled = "Недоступно",
                readOnly = "Только чтение",
                error = "Ошибка",
                indeterminate = "Неопределённое состояние"
            }
            : new
            {
                disabled = "Disabled",
                readOnly = "Read-only",
                error = "Error",
                indeterminate = "Indeterminate"
            };
        await page.GotoAsync(
            BaseUrl + "/components/checkbox",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();
        var demo = page.GetByTestId("checkbox-admiral-demo");
        var field = demo.Locator(".admiral-checkbox-field");
        var checkbox = demo.GetByRole(AriaRole.Checkbox, new LocatorGetByRoleOptions { Name = label, Exact = true });

        // Assert: verify the initial accessible name, value and dimension.
        Assert.That(
            await checkbox.CountAsync(),
            Is.EqualTo(1),
            "Admiral checkbox has its visible label as accessible name");
        Assert.That(await checkbox.GetAttributeAsync("aria-checked"), Is.EqualTo("false"));
        Assert.That(await field.GetAttributeAsync("class"), Does.Match(new Regex("admiral-checkbox-field--m")));

        // Act: enter the checkbox with keyboard focus.
        await page.Keyboard.PressAsync("Tab");
        await checkbox.FocusAsync();

        // Assert
        Assert.That(
            await checkbox.EvaluateAsync<bool>("element => element.matches(':focus-visible')"),
            Is.EqualTo(true));
        Assert.That(await checkbox.GetCssAsync("outline-style"), Is.Not.EqualTo("none"));

        // Act: toggle the value with Space.
        await page.Keyboard.PressAsync("Space");

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .rz-chkbox')
                ?.getAttribute('aria-checked') === 'true'
            """);

        Assert.That(
            await demo.GetByTestId("checkbox-admiral-result").InnerTextAsync(),
            Does.Match(new Regex("(?:Changes|Изменений):\\s*1")));

        // Act: switch to the small dimension.
        await demo.GetByRole(
            AriaRole.Combobox,
            new LocatorGetByRoleOptions { Name = (language == "ru") ? "Размер" : "Dimension" }).SelectOptionAsync("S");

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
                ?.classList.contains('admiral-checkbox-field--s')
            """);

        // Act: restore the medium dimension.
        await demo.GetByRole(
            AriaRole.Combobox,
            new LocatorGetByRoleOptions { Name = (language == "ru") ? "Размер" : "Dimension" }).SelectOptionAsync("M");

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
                ?.classList.contains('admiral-checkbox-field--m')
            """);

        // Act: disable the checkbox.
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.disabled, Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
                ?.classList.contains('admiral-checkbox-field--disabled')
            """);

        Assert.That(await field.GetAttributeAsync("class"), Does.Match(new Regex("admiral-checkbox-field--disabled")));
        Assert.That(await checkbox.GetAttributeAsync("aria-disabled"), Is.EqualTo("true"));
        Assert.That(await checkbox.GetAttributeAsync("tabindex"), Is.EqualTo("-1"));
        Assert.That(
            await demo.GetByTestId("checkbox-admiral-result").InnerTextAsync(),
            Does.Match(new Regex("(?:Changes|Изменений):\\s*1")));

        // Act: replace the disabled state with read-only.
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.disabled, Exact = true }).UncheckAsync();
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.readOnly, Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
                ?.classList.contains('admiral-checkbox-field--readonly')
            """);

        Assert.That(await field.GetAttributeAsync("class"), Does.Match(new Regex("admiral-checkbox-field--readonly")));

        // Act: attempt to change the read-only value.
        await checkbox.FocusAsync();
        await page.Keyboard.PressAsync("Space");

        // Assert
        Assert.That(await checkbox.GetAttributeAsync("aria-checked"), Is.EqualTo("true"));
        Assert.That(
            await demo.GetByTestId("checkbox-admiral-result").InnerTextAsync(),
            Does.Match(new Regex("(?:Changes|Изменений):\\s*1")));

        // Act: apply error styling to a checked checkbox.
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.readOnly, Exact = true }).UncheckAsync();
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.error, Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .admiral-checkbox-field')
                ?.classList.contains('admiral-checkbox-field--error')
            """);

        Assert.That(await field.GetAttributeAsync("class"), Does.Match(new Regex("admiral-checkbox-field--error")));
        Assert.That(
            await field.Locator(".rz-chkbox-box.rz-state-active").GetCssAsync("box-shadow"),
            Is.Not.EqualTo("none"),
            "checked + error keeps a visible error ring");

        // Act: show the indeterminate value.
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.indeterminate, Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-admiral-demo"] .rz-chkbox')
                ?.getAttribute('aria-checked') === 'mixed'
            """);

        // Act: restore the checked value.
        await demo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.indeterminate, Exact = true }).UncheckAsync();

        // Assert
        await demo.Locator(".rz-chkbox[aria-checked=\"true\"]").WaitForAsync();

        // Act: activate the visible label.
        await field.Locator(".admiral-checkbox-field__text").ClickAsync();

        // Assert
        await demo.Locator(".rz-chkbox[aria-checked=\"false\"]").WaitForAsync();

        Assert.That(
            await checkbox.EvaluateAsync<bool>("element => element === document.activeElement"),
            Is.True,
            "Label activation focuses the exposed checkbox");

        // Act: toggle once more using the keyboard.
        await page.Keyboard.PressAsync("Space");

        // Assert
        await demo.Locator(".rz-chkbox[aria-checked=\"true\"]").WaitForAsync();

        Assert.That(
            await demo.GetByTestId("checkbox-admiral-result").InnerTextAsync(),
            Does.Match(new Regex("(?:Changes|Изменений):\\s*3")),
            "Label click and subsequent Space each emit exactly one change");

        // Act: switch to the independent direct Radzen example.
        await page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('.component-doc-tabs [data-doc-tab="theme"]')
                ?.getAttribute('aria-pressed') === 'true'
            """);

        var direct = page.GetByTestId("checkbox-radzen-demo");

        // Assert
        var directCheckbox = direct.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = label, Exact = true });

        Assert.That(await directCheckbox.CountAsync(), Is.EqualTo(1), "Direct Radzen checkbox has an accessible name");
        Assert.That(await directCheckbox.GetAttributeAsync("aria-checked"), Is.EqualTo("false"));

        // Act: focus the direct control using the keyboard.
        await page.Keyboard.PressAsync("Tab");
        await directCheckbox.FocusAsync();

        // Assert
        Assert.That(
            await directCheckbox.EvaluateAsync<bool>("element => element.matches(':focus-visible')"),
            Is.EqualTo(true));

        // Act: toggle the direct control.
        await page.Keyboard.PressAsync("Space");

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-radzen-demo"] .rz-chkbox')
                ?.getAttribute('aria-checked') === 'true'
            """);

        Assert.That(
            await direct.GetByTestId("checkbox-radzen-result").InnerTextAsync(),
            Does.Match(new Regex("(?:Changes|Изменений):\\s*1")));

        // Act: disable the direct control.
        await direct.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = controls.disabled, Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('[data-testid="checkbox-radzen-demo"] .rz-chkbox')
                ?.getAttribute('aria-disabled') === 'true'
            """);

        Assert.That(await directCheckbox.GetAttributeAsync("aria-disabled"), Is.EqualTo("true"));
        Assert.That(
            await direct.GetByTestId("checkbox-radzen-result").InnerTextAsync(),
            Does.Match(new Regex("(?:Changes|Изменений):\\s*1")));

        // Act: open the composite checkbox group.
        await page.GotoAsync(BaseUrl + "/components/checkbox-group");

        // Assert
        await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();

        var group = page.Locator(".admiral-checkbox-composite-group");
        var boxes = group.GetByRole(AriaRole.Checkbox);
        var parent = boxes.First;

        // Act
        await boxes.Nth(1).PressAsync("Space");

        // Assert
        await group.Locator(".rz-chkbox[aria-checked=\"mixed\"]").WaitForAsync();

        // Act: toggle the mixed parent to update its children.
        await parent.PressAsync("Space");

        // Assert
        await page.WaitForFunctionAsync(
            """
            () =>
              document.querySelectorAll('.admiral-checkbox-composite-group .rz-chkbox[aria-checked="true"]')
                .length === 5
            """);

        // Act: disable the composite group.
        await page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Disabled", Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync(
            """
            () =>
              document.querySelectorAll(
                '.admiral-checkbox-composite-group .rz-chkbox[aria-disabled="true"][tabindex="-1"]',
              ).length === 5
            """);
    }
}
