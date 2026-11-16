namespace Admiral.Playwright.Components.Select;

[Category("Select")]
public sealed class SelectTests : BrowserTest
{
    [TestCase("admiral", "ru", 1440, 900)]
    [TestCase("admiral", "ru", 390, 780)]
    [TestCase("admiral", "en", 1440, 900)]
    [TestCase("admiral", "en", 390, 780)]
    [TestCase("admiral-dark", "ru", 1440, 900)]
    [TestCase("admiral-dark", "ru", 390, 780)]
    [TestCase("admiral-dark", "en", 1440, 900)]
    [TestCase("admiral-dark", "en", 390, 780)]
    public async Task SingleAndMultipleSelectionAsync(string theme, string culture, int width, int height)
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
        await page.GotoAsync(BaseUrl + "/components/select");
        var playground = page.Locator(".admiral-component-playground").First;
        await playground.WaitForAsync();
        await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();
        var select = playground.Locator(".rz-dropdown").First;
        var visibleLabel = playground.Locator(".admiral-input-field__label").First;
        var labelText = (await visibleLabel.InnerTextAsync()).Trim();
        var labelId = await visibleLabel.GetAttributeAsync("id");

        // Assert
        Assert.That(labelId, Is.Not.Null.And.Not.Empty, "SelectField visible label has a stable id");
        Assert.That(
            await select.GetAttributeAsync("aria-labelledby"),
            Is.EqualTo(labelId),
            "SelectField combobox is named by its visible label");
        Assert.That(
            await playground.GetByRole(
                AriaRole.Combobox,
                new LocatorGetByRoleOptions { Name = labelText }).CountAsync(),
            Is.EqualTo(1),
            "SelectField has one computed accessible name");

        // Act: activate the combobox through its visible label.
        await visibleLabel.ClickAsync();

        // Assert
        await page.WaitForFunctionAsync(
            "id => document.activeElement?.id === id",
            await select.GetAttributeAsync("id"));

        Assert.That(
            await select.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(true),
            "Clicking the visible SelectField label focuses its combobox");
        Assert.That(
            await playground.EvaluateAsync<bool>(
                "element => document.activeElement?.getAttribute('aria-hidden') === 'true'"),
            Is.EqualTo(false),
            "Label click must not focus Radzen aria-hidden helper input");

        // Act: leave the combobox with Tab.
        await page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(
            await select.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(false),
            "Tab leaves the SelectField combobox");

        // Act: return with Shift+Tab.
        await page.Keyboard.PressAsync("Shift+Tab");

        // Assert
        Assert.That(
            await select.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(true),
            "Shift+Tab returns to the same combobox");

        // Act: open the popup using the keyboard.
        await select.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        var popup = page.Locator(".rz-dropdown-panel:visible").Last;

        // Assert
        await popup.WaitForAsync();
        var box = await popup.GetBoundsAsync();

        Assert.That(box.Width > 0 && box.Height > 0, Is.True, "Select popup rendered");
        Assert.That(
            box.X >= -1 && box.X + box.Width <= viewport.width + 1 && box.Y >= -1
                && box.Y + box.Height <= viewport.height + 1,
            Is.True,
            $"{theme}/{culture}/{viewport.width}: Select popup fits viewport {JsonSerializer.Serialize(box)}");

        var triggerBox = await select.GetBoundsAsync();

        Assert.That(
            box.Y + box.Height <= triggerBox.Y + 2 || box.Y >= triggerBox.Y + triggerBox.Height - 2,
            Is.True,
            $"{theme}/{culture}/{viewport.width}: Select popup must not overlap its trigger");
        Assert.That(
            await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"),
            Is.True,
            "Select popup does not create horizontal overflow");

        // Act: dismiss the popup with Escape.
        await page.Keyboard.PressAsync("Escape");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        await page.WaitForFunctionAsync(
            "id => document.activeElement?.id === id",
            await select.GetAttributeAsync("id"));

        Assert.That(
            await select.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(true),
            "Escape closes SelectField popup and restores focus to its combobox");

        // Act: verify the normal tab order is restored.
        await page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(
            await select.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(false),
            "Tab moves forward from a closed SelectField popup");

        // Act: reopen the list for keyboard selection.
        await select.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        // Assert
        await popup.WaitForAsync();

        // Act: move through enabled options.
        await page.Keyboard.PressAsync("ArrowDown");

        // Assert
        Assert.That(
            await popup.Locator(".rz-state-highlight").CountAsync() > 0,
            Is.True,
            "ArrowDown highlights a selectable option");

        // Act
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        await Assertions.Expect(select).ToHaveAttributeAsync("aria-expanded", "false");

        // Act: select a value with the mouse.
        await select.ClickAsync();

        // Assert
        await popup.WaitForAsync();

        // Act
        await popup.GetByText("Казань", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () =>
              document
                .querySelector('.admiral-component-playground [role="status"]')
                ?.textContent?.includes('KZN')
            """);

        Assert.That(
            await playground.Locator("[role=\"status\"]").InnerTextAsync(),
            Does.Match(new Regex("KZN.*[1-9]")),
            "Selection callback is visible in playground");

        var afterSelection = await playground.Locator("[role=\"status\"]").InnerTextAsync();

        // Act: select the same value again.
        await select.ClickAsync();

        // Assert
        await popup.WaitForAsync();

        // Act
        await popup.GetByText("Казань", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        Assert.That(
            await playground.Locator("[role=\"status\"]").InnerTextAsync(),
            Is.EqualTo(afterSelection),
            "Selecting the current value again does not emit a duplicate callback");

        // Act: clear the selected value.
        await select.Locator(".rz-dropdown-clear-icon").ClickAsync();

        // Assert
        await page.WaitForFunctionAsync(
            """
            (previous) =>
              document.querySelector('.admiral-component-playground [role="status"]')?.textContent !== previous
            """,
            afterSelection);
        var countBeforeClear = ReadChangeCount(afterSelection);
        var countAfterClear = ReadChangeCount(await playground.Locator("[role=\"status\"]").InnerTextAsync());

        Assert.That(
            countAfterClear,
            Is.EqualTo(countBeforeClear + 1),
            "Clearing a selected value emits exactly one callback");

        // Assert: inspect error styling independently of the selected value.
        var errorField = page.Locator(".admiral-input-field").Filter(
            new LocatorFilterOptions { Has = page.Locator("label.admiral-input-field__label").Filter(new LocatorFilterOptions { HasText = "Error" }) }).First;

        Assert.That(
            await errorField.Locator(".rz-dropdown").GetAttributeAsync("aria-invalid"),
            Is.EqualTo("true"),
            "SelectField error status is exposed to assistive technology");

        // Arrange: locate the grouped selection example.
        var grouped = page.Locator(".admiral-input-field").Filter(
            new LocatorFilterOptions { Has = page.Locator(".admiral-input-field__label").GetByText(new Regex("^(Grouped|С группировкой)$")) }).GetByRole(AriaRole.Combobox);

        // Act
        await grouped.ClickAsync();

        // Assert
        await popup.WaitForAsync();

        Assert.That(
            await popup.Locator(".admiral-dropdown-group").AllTextContentsAsync(),
            Is.EqualTo(new[] { "Россия", "Беларусь", "Казахстан" }),
            "Single Select renders group headings in source order");

        // Act: select an option from a group.
        await popup.GetByText("Алматы", new LocatorGetByTextOptions { Exact = false }).ClickAsync();

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        Assert.That(await grouped.InnerTextAsync(), Does.Match(new Regex("ALA.*Алматы", RegexOptions.Singleline)));

        // Act & assert: repeat popup and callback checks in single and multiple modes.
        foreach (var multiple in new[]
        {
            false,
            true
        })
        {
            // Act
            if (multiple)
            {
                await playground.GetByRole(
                    AriaRole.Checkbox,
                    new LocatorGetByRoleOptions { Name = "Multiple", Exact = true }).CheckAsync();
                await select.And(page.Locator(".admiral-select__control:not(.admiral-dropdown)")).WaitForAsync();
            }

            var panel = page.Locator(multiple ? ".rz-multiselect-panel:visible" : ".rz-dropdown-panel:visible");

            // Act
            await select.ScrollIntoViewIfNeededAsync();
            await select.FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await panel.WaitForAsync();

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            await page.WaitForFunctionAsync(
                "id => document.activeElement?.id === id",
                await select.GetAttributeAsync("id"));
            if (viewport.width == 1440)
            {
                // Act
                await page.Keyboard.PressAsync("Enter");

                // Assert
                await panel.WaitForAsync();

                // Act
                await page.SetViewportSizeAsync(390, viewport.height);

                // Assert
                await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
                await page.WaitForFunctionAsync(
                    "id => document.activeElement?.id === id",
                    await select.GetAttributeAsync("id"));

                // Act
                await page.Keyboard.PressAsync("Enter");

                // Assert
                await panel.WaitForAsync();
                var reopened = await panel.GetBoundsAsync();

                Assert.That(
                    reopened.X >= -1 && reopened.X + reopened.Width <= 391,
                    Is.True,
                    "Select reopens within the narrowed viewport");

                // Act
                await page.Keyboard.PressAsync("Escape");

                // Assert
                await panel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

                // Act
                await page.SetViewportSizeAsync(viewport.width, viewport.height);
            }

            // Act
            if (multiple)
            {
                // Act
                await select.ClickAsync();

                // Assert
                await panel.WaitForAsync();

                var result = playground.Locator("[role=\"status\"]");

                // Assert
                await result.GetByText(new Regex("MOW, KZN")).WaitForAsync();

                // Act
                await panel.GetByText("Минск", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

                // Assert
                await result.GetByText(new Regex("MOW, KZN, MSQ")).WaitForAsync();

                // Act
                await panel.GetByText("Казань", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

                // Assert
                await result.GetByText(new Regex("MOW, MSQ")).WaitForAsync();
            }
        }

        // Arrange: load the independent direct Radzen dropdown.
        await page.GotoAsync(BaseUrl + "/components/drop-down");

        // Assert
        await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();

        // Act
        await page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

        var direct = page.Locator("[data-testid=\"drop-down-radzen-demo\"] .rz-dropdown").First;

        // Assert
        await direct.WaitForAsync();

        // Act: focus and open the direct control using the keyboard.
        await direct.FocusAsync();

        // Assert
        Assert.That((await direct.GetBoundsAsync())?.Width > 0, Is.True, "Direct RadzenDropDown renders");

        // Act
        await page.Keyboard.PressAsync("Enter");

        var directPopup = page.Locator(".rz-dropdown-panel:visible").Last;

        // Assert
        await directPopup.WaitForAsync();
        var directBox = await direct.GetBoundsAsync();
        var directPanelBox = await directPopup.GetBoundsAsync();

        Assert.That(
            directPanelBox.Y + directPanelBox.Height <= directBox.Y + 2
                || directPanelBox.Y >= directBox.Y + directBox.Height - 2,
            Is.True,
            $"{theme}/{culture}/{viewport.width}: direct RadzenDropDown popup must not overlap its trigger");

        // Act: dismiss the direct popup with Escape.
        await page.Keyboard.PressAsync("Escape");

        // Assert
        await directPopup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Act: reopen the direct control with the mouse.
        await direct.ClickAsync();
        await directPopup.GetByText("Казань", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

        // Assert
        await page.Locator("[data-testid=\"drop-down-radzen-demo\"] [role=\"status\"]").GetByText(new Regex("Казань")).WaitForAsync();
    }

    private static int ReadChangeCount(string status)
    {
        var match = Regex.Match(status, @"(?:Changes|Изменений):\s*(\d+)");
        Assert.That(match.Success, Is.True, $"Missing change count in status: {status}");
        return int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
