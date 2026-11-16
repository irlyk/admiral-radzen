namespace Admiral.Playwright.Documentation;

[Category("Documentation")]
public sealed class DocumentationTests : BrowserTest
{
    [TestCase("admiral", "ru")]
    [TestCase("admiral", "en")]
    [TestCase("admiral-dark", "ru")]
    [TestCase("admiral-dark", "en")]
    public async Task DocumentationPagesExposeExamplesAndApisAsync(string theme, string language)
    {
        // Arrange: create the themed and localized documentation context.
        var (context, page) = await NewPageAsync(theme, language, 1280, 800);
        var routes = new[]
        {
            "/components/button",
            "/components/icon-button",
            "/components/text-button",
            "/components/button-groups",
            "/components/icon-button-group",
            "/components/floating-button-menu",
            "/components/action-bar",
            "/components/switch",
            "/components/checkbox",
            "/components/checkbox-group",
            "/components/text-input",
            "/components/text-area",
            "/components/number-input",
            "/components/select",
            "/components/input-field",
            "/components/drop-down",
            "/components/date-range-picker",
            "/components/tree-select",
            "/components/slider",
            "/components/phone-number-input",
            "/components/file-input",
            "/components/global-search",
            "/components/link",
            "/components/menu",
            "/components/drawer",
            "/components/stepper",
            "/components/tree",
            "/components/pagination",
            "/components/typography",
            "/components/divider",
            "/components/badge",
            "/components/chips",
            "/components/pills",
            "/components/tag",
            "/components/lists",
            "/components/scroll-container",
            "/components/carousel",
            "/components/image-viewer",
            "/tables",
            "/components/avatar",
            "/components/status-indicator",
            "/components/grid",
            "/components/skeleton",
            "/components/tooltip",
            "/components/hint",
            "/components/toast",
            "/components/progress",
            "/components/spinner"
        };

        // Act & assert: inspect component examples, tabs and API documentation.
        foreach (var route in routes)
        {
            {
                // Act
                var response = await page.GotoAsync(
                    BaseUrl + route,
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

                // Assert
                Assert.That(response!.Status, Is.EqualTo(200), $"{route}: HTTP status");
                await page.Locator(".admiral-component-page").WaitForAsync();

                Assert.That(
                    await page.Locator(".admiral-component-page").CountAsync(),
                    Is.EqualTo(1),
                    $"{route}: page");

                // Assert
                foreach (var tab in new[]
                {
                    "admiral",
                    "api",
                    "theme"
                })
                {
                    // Assert
                    Assert.That(
                        await page.Locator(
                            $".admiral-component-page .component-doc-tabs [data-doc-tab=\"{tab}\"]").CountAsync(),
                        Is.EqualTo(1),
                        $"{route}: {tab} tab");
                }

                // Assert
                Assert.That(
                    await page.Locator(
                        ".admiral-component-page a[href*=\"admiralds.github.io/react-ui/\"], .admiral-component-page a[href*=\"github.com/AdmiralDS/react-ui\"]").CountAsync() > 0,
                    Is.EqualTo(true),
                    $"{route}: official link");
            }
        }

        // Act & assert: inspect the icon catalog.
        {
            // Act
            var response = await page.GotoAsync(
                BaseUrl + "/icons",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Assert
            Assert.That(response!.Status, Is.EqualTo(200), "/icons: HTTP status");

            var section = page.Locator("#icon-component-heading").Locator("..");

            await Assertions.Expect(
                section.GetByRole(
                    AriaRole.Heading,
                    new LocatorGetByRoleOptions { Name = "AdmiralIcon", Exact = true })).ToHaveCountAsync(1);
            await Assertions.Expect(section.Locator("a[href*=\"admiralds.github.io/react-ui/\"]")).ToHaveCountAsync(1);

            // Assert
            foreach (var tab in new[]
            {
                "admiral",
                "api",
                "theme"
            })
            {
                // Assert
                Assert.That(
                    await section.Locator($"[data-doc-tab=\"{tab}\"]").CountAsync(),
                    Is.EqualTo(1),
                    $"/icons: {tab} tab");
            }

            await page.WaitForTimeoutAsync(600);

            // Act
            await section.Locator("[data-doc-tab=\"theme\"]").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('#icon-component-heading')
                    ?.parentElement?.querySelector('[data-doc-tab="theme"]')
                    ?.getAttribute('aria-pressed') === 'true'
                """);
            await section.Locator(".admiral-component-playground__preview .admiral-icon").WaitForAsync();

            // Act
            await section.Locator("[data-doc-tab=\"admiral\"]").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('#icon-component-heading')
                    ?.parentElement?.querySelector('[data-doc-tab="admiral"]')
                    ?.getAttribute('aria-pressed') === 'true'
                """);

            // Act
            await section.Locator("select").SelectOptionAsync("ExtraLarge");

            // Assert
            await page.WaitForFunctionAsync(
                """
                () =>
                  document
                    .querySelector('#icon-component-heading')
                    ?.parentElement?.querySelector('.admiral-component-playground__preview .admiral-icon')
                    ?.getBoundingClientRect().width === 32
                """);

            // Act
            await section.Locator("[data-doc-tab=\"api\"]").ClickAsync();

            // Assert
            await section.GetByText("Razor · AdmiralIcon").WaitForAsync();
            await page.Locator(".admiral-icon-grid[role=\"list\"]").WaitForAsync();

            var searchName = (language == "ru") ? "Поиск иконок" : "Search icons";
            var categoryName = (language == "ru") ? "Категория иконок" : "Icon category";

            // Assert
            await page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions { Name = searchName, Exact = true }).WaitForAsync();
            await page.GetByRole(
                AriaRole.Combobox,
                new PageGetByRoleOptions { Name = categoryName, Exact = true }).WaitForAsync();
            await page.Locator(".admiral-icon-grid [role=\"listitem\"] button[aria-pressed]").First.WaitForAsync();
        }

        // Act & assert: inspect composite-checkbox examples.
        {
            // Act
            await page.GotoAsync(
                BaseUrl + "/components/checkbox-group",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            var labels = (language == "ru") ? new[]
            {
                "Города",
                "Москва",
                "Воронеж",
                "Самара",
                "Омск"
            }
            : new[]
            {
                "Cities",
                "Moscow",
                "Voronezh",
                "Samara",
                "Omsk"
            };

            // Assert
            var preview = page.Locator(".admiral-component-playground__preview");

            // Assert
            foreach (var name in labels)
            {
                // Assert
                Assert.That(
                    await preview.GetByRole(
                        AriaRole.Checkbox,
                        new LocatorGetByRoleOptions { Name = name, Exact = true }).CountAsync(),
                    Is.EqualTo(1),
                    $"/components/checkbox-group Admiral: {name} accessible name");
            }

            // Act
            await page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('.component-doc-tabs [data-doc-tab="theme"]')
                    ?.getAttribute('aria-pressed') === 'true'
                """);

            // Assert
            foreach (var name in labels)
            {
                // Assert
                Assert.That(
                    await preview.GetByRole(
                        AriaRole.Checkbox,
                        new LocatorGetByRoleOptions { Name = name, Exact = true }).CountAsync(),
                    Is.EqualTo(1),
                    $"/components/checkbox-group direct Radzen: {name} accessible name");
            }
        }

        // Act & assert: compare the dedicated button-family pages.
        foreach (var route in new[]
        {
            "/components/icon-button",
            "/components/text-button",
            "/components/button-groups",
            "/components/icon-button-group"
        })
        {
            {
                // Act
                await page.GotoAsync(
                    BaseUrl + route,
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForTimeoutAsync(600);

                var preview = page.Locator(".admiral-component-playground__preview").First;
                var result = preview.Locator("output");

                // Assert
                var before = await result.InnerTextAsync();

                // Act
                await preview.GetByRole(AriaRole.Button).First.FocusAsync();
                await page.Keyboard.PressAsync("Enter");

                // Assert
                await page.WaitForFunctionAsync(
                    """
                    (previous) => {
                      const text = document.querySelector('.admiral-component-playground__preview output')?.textContent;
                      return text && text !== previous;
                    }
                    """,
                    before);
            }
        }

        // Act: exercise secondary button-group styling.
        await page.GotoAsync(
            BaseUrl + "/components/button-groups",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".admiral-component-controls select").Nth(1).SelectOptionAsync("Secondary");

        // Assert
        await page.WaitForFunctionAsync(
            """
            () =>
              document
                .querySelector('.admiral-component-playground__preview .admiral-button-group > button')
                ?.classList.contains('admiral-button--secondary')
            """);

        // Act: switch the group to tertiary styling.
        await page.Locator(".admiral-component-controls select").Nth(1).SelectOptionAsync("Tertiary");

        // Assert
        await page.WaitForFunctionAsync(
            """
            () =>
              document
                .querySelector('.admiral-component-playground__preview .admiral-button-group > button')
                ?.classList.contains('admiral-button--tertiary')
            """);

        // Act: apply a custom border radius.
        await page.Locator("[data-testid=\"smoke-border-radius\"]").SelectOptionAsync("10");

        // Assert
        await page.WaitForFunctionAsync(
            """
            () =>
              getComputedStyle(document.documentElement).getPropertyValue('--admiral-radius-medium').trim() ===
              '0.625rem'
            """);

        // Act: exercise the small icon-button group.
        await page.GotoAsync(
            BaseUrl + "/components/icon-button-group",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".admiral-component-controls select").First.SelectOptionAsync("S");

        // Assert
        await page.WaitForFunctionAsync(
            """
            () =>
              document
                .querySelector('.admiral-component-playground__preview .admiral-icon-button-group > button')
                ?.classList.contains('rz-button-xs')
            """);

        // Act & assert: inspect controls whose size changes geometry.
        foreach (var (route, control) in new[]
        {
            ("/components/chips", ".admiral-chip"),
            ("/components/action-bar", ".admiral-action-bar button")
        })
        {
            {
                // Act
                await page.GotoAsync(
                    BaseUrl + route,
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForTimeoutAsync(600);

                var preview = page.Locator(".admiral-component-playground__preview").First;
                var result = preview.Locator("output").First;

                // Assert
                var before = await result.InnerTextAsync();

                // Act
                await preview.Locator(control).First.FocusAsync();
                await page.Keyboard.PressAsync("Enter");

                // Assert
                await page.WaitForFunctionAsync(
                    """
                    (previous) => {
                      const text = document.querySelector('.admiral-component-playground__preview output')?.textContent;
                      return text && text !== previous;
                    }
                    """,
                    before);
            }
        }

        // Act: switch to a mobile viewport.
        await page.SetViewportSizeAsync(390, 844);
        await page.GotoAsync(BaseUrl + "/icons", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Assert
        await page.Locator(".admiral-icon-gallery").WaitForAsync();

        // Assert: the icon catalog stays inside the narrow viewport.
        var iconsOverflow = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth - innerWidth");

        Assert.That(iconsOverflow <= 1, Is.True, $"/icons: horizontal overflow {iconsOverflow}px");

        // Act & assert: revisit every component page at mobile width.
        foreach (var route in routes)
        {
            {
                // Act
                await page.GotoAsync(
                    BaseUrl + route,
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

                // Assert
                await page.Locator(".admiral-component-page").WaitForAsync();
                var overflow = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth - innerWidth");

                Assert.That(overflow <= 1, Is.True, $"{route}: horizontal overflow {overflow}px");
            }
        }

        // Act
        TestContext.Progress.WriteLine(
            Convert.ToString(
                $"{theme} {language}: {routes.Length} component routes + /icons, 5 keyboard callbacks, {routes.Length} component mobile pages + /icons checked"));
    }
}
