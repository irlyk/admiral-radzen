namespace Admiral.Playwright.Integration.NuGet;

[Category("Consumer")]
public sealed class ConsumerTests : BrowserTest
{
    [Test]
    public async Task PackagedComponentsAndDirectControlsWorkAsync()
    {
        // Arrange: publish an isolated package consumer and monitor asset failures.
        var host = Own(await AppHost.StartConsumerAsync());
        var (context, page) = await NewPageAsync(url: host.Url);
        var consumerUrl = host.Url;
        var assetErrors = new List<string>();
        page.Response += (_, response) =>
        {
            if (response!.Status >= 400)
            {
                assetErrors.Add($"{response!.Status} {response.Url}");
            }
        };

        // Act: open the consumer application.
        await page.GotoAsync(consumerUrl);
        await page.GetByRole(
            AriaRole.Heading,
            new PageGetByRoleOptions { Name = "Package consumer validation" }).WaitForAsync();
        await page.Locator(".admiral-icon svg").WaitForAsync();

        // Assert: the packaged light theme is applied.
        var lightHref = await page.Locator("[data-testid=\"theme-link\"]").GetAttributeAsync("href");

        // Assert
        Assert.That(lightHref, Does.Match(new Regex("admiral-base\\.css$")));

        var button = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Count clicks" });
        Task<string> ReadButtonColorAsync() => button.GetCssAsync("background-color");
        var lightButtonColor = await ReadButtonColorAsync();
        await page.WaitForTimeoutAsync(800);

        // Act: switch the package consumer to the dark theme.
        await page.Locator("[data-testid=\"theme-toggle\"]").ClickAsync();

        // Assert
        await page.GetByText("Dark", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        Assert.That(
            await page.Locator("[data-testid=\"theme-link\"]").GetAttributeAsync("href"),
            Does.Match(new Regex("admiral-dark-base\\.css$")));
        await page.WaitForFunctionAsync(
            "color => getComputedStyle(document.querySelector('.rz-button')).backgroundColor !== color",
            lightButtonColor);
        var darkButtonColor = await ReadButtonColorAsync();

        Assert.That(
            darkButtonColor,
            Is.Not.EqualTo(lightButtonColor),
            "Packaged light/dark CSS should alter direct RadzenButton styling");

        // Act: invoke the packaged button callback.
        await button.ClickAsync();

        // Assert
        await page.GetByText("Clicks: 1", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act: edit and commit a direct text input.
        await page.GetByPlaceholder("Search").FillAsync("package works");
        await page.GetByPlaceholder("Search").PressAsync("Tab");

        // Assert
        await page.GetByText("Query: package works", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Act: switch back to the light theme.
        await page.Locator("[data-testid=\"theme-toggle\"]").ClickAsync();

        // Assert
        await page.GetByText("Light", new PageGetByTextOptions { Exact = true }).WaitForAsync();

        // Assert: required framework and package assets are served.
        foreach (var asset in new[]
        {
            "/_framework/blazor.web.js",
            "/_content/Radzen.Blazor/Radzen.Blazor.js",
            "/_content/Radzen.Admiral.Icons/admiral-radzen-icons.css",
            "/_content/Radzen.Admiral.Icons/admiral-radzen-icons.js",
            "/_content/Radzen.Admiral.Icons/admiral-icons/system/search-outline.svg",
            "/_content/Radzen.Admiral.Themes/css/admiral-base.css",
            "/_content/Radzen.Admiral.Themes/css/admiral-dark-base.css"
        })
        {
            // Assert
            Assert.That((await page.APIRequest.GetAsync(consumerUrl + asset)).Status, Is.EqualTo(200), asset);
        }

        // Act & assert: exercise pagination in both themes.
        foreach (var theme in new[]
        {
            "Light",
            "Dark"
        })
        {
            // Act
            await page.GotoAsync(consumerUrl + "/pagination-regression");

            // Assert
            await page.GetByRole(
                AriaRole.Heading,
                new PageGetByRoleOptions { Name = "Pagination callback regression" }).WaitForAsync();
            await page.WaitForTimeoutAsync(800);

            // Act
            if (theme == "Dark")
            {
                await page.Locator("[data-testid=\"pagination-theme-toggle\"]").ClickAsync();
                await page.GetByText("Dark", new PageGetByTextOptions { Exact = true }).WaitForAsync();
            }

            // Assert
            var themeHref = await page.Locator("[data-testid=\"pagination-theme-link\"]").GetAttributeAsync("href");

            Assert.That(
                themeHref,
                Does.Match(
                    (theme == "Dark") ? new Regex("admiral-dark-base\\.css$") : new Regex("admiral-base\\.css$")));

            var model = page.Locator("[data-testid=\"pagination-model\"]");

            Assert.That(
                (await model.InnerTextAsync()).Trim(),
                Is.EqualTo("Page: 2 · PageSize: 10 · PageChanged: 0 · PageSizeChanged: 0"));

            var size = page.Locator(".admiral-pagination-one__page-size .rz-dropdown");

            // Act
            await size.FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await page.Locator(".rz-dropdown-panel:visible").WaitForAsync();

            // Act
            await page.Keyboard.PressAsync("ArrowDown");

            // Assert
            await page.WaitForFunctionAsync(
                """
                () =>
                  [...document.querySelectorAll('.rz-dropdown-panel')]
                    .filter((panel) => panel.getBoundingClientRect().width > 0)
                    .some((panel) => panel.querySelector('.rz-state-highlight')?.textContent?.trim() === '20')
                """);

            // Act
            await page.Keyboard.PressAsync("Space");

            // Assert
            await page.WaitForFunctionAsync(
                """
                () =>
                  document.querySelector('[data-testid="pagination-model"]')?.textContent?.includes('PageSize: 20')
                """);

            Assert.That(
                (await model.InnerTextAsync()).Trim(),
                Is.EqualTo("Page: 1 · PageSize: 20 · PageChanged: 1 · PageSizeChanged: 1"),
                $"{theme}: changing size from 10 to 20 resets page with one callback");
        }

        // Arrange: retain direct-control colors for the light/dark comparison.
        string[]? directLightColors = null;

        // Act & assert: exercise phone inputs in both themes.
        foreach (var theme in new[]
        {
            "Light",
            "Dark"
        })
        {
            // Act
            await page.GotoAsync(consumerUrl + "/phone-regression");

            // Assert
            await page.GetByRole(
                AriaRole.Heading,
                new PageGetByRoleOptions { Name = "Phone package regression" }).WaitForAsync();
            await page.WaitForTimeoutAsync(800);

            // Act
            if (theme == "Dark")
            {
                await page.GetByTestId("phone-theme-toggle").ClickAsync();
                await page.GetByTestId("phone-theme").GetByText("Dark").WaitForAsync();
            }

            // Assert
            Assert.That(
                await page.GetByTestId("phone-theme-link").GetAttributeAsync("href"),
                Does.Match(
                    (theme == "Dark") ? new Regex("admiral-dark-base\\.css$") : new Regex("admiral-base\\.css$")));

            var admiral = page.GetByTestId("phone-admiral");

            // Act
            await admiral.Locator("input[inputmode=\"tel\"]").FillAsync("+7 777 123-45-68");
            await admiral.Locator("input[inputmode=\"tel\"]").PressAsync("Tab");

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('[data-testid="phone-model"]')
                    ?.textContent?.includes('Value: +7 777 123-45-68')
                """);

            Assert.That(
                await page.GetByTestId("phone-model").InnerTextAsync(),
                Does.Match(new Regex("Country: KAZ · ValueChanged: 1 · CountryChanged: 0")),
                $"{theme}: editing a shared +7 dial code retains KAZ");

            var digits = page.GetByTestId("phone-digits");

            // Act
            await digits.Locator("input[inputmode=\"tel\"]").FillAsync("+7 777 123 45 69");
            await digits.Locator("input[inputmode=\"tel\"]").PressAsync("Tab");

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('[data-testid="phone-digits-model"]')
                    ?.textContent?.includes('Value: 77771234569')
                """);

            Assert.That(
                await page.GetByTestId("phone-digits-model").InnerTextAsync(),
                Does.Match(new Regex("Country: KAZ · CountryChanged: 0")),
                $"{theme}: digits mode retains KAZ with shared +7 code");

            var direct = page.GetByTestId("phone-direct-radzen");
            var directSelect = direct.Locator(".rz-dropdown");

            // Assert
            var directColors = await direct.EvaluateAsync<string[]>(
                """
                (element) => {
                  const dropDown = element.querySelector('.rz-dropdown');
                  const textBox = element.querySelector('.rz-textbox');
                  return [getComputedStyle(dropDown).backgroundColor, getComputedStyle(textBox).backgroundColor];
                }
                """);
            if (theme == "Light")
            {
                directLightColors = directColors;
            }
            else
            {
                Assert.That(
                    directColors,
                    Is.Not.EqualTo(directLightColors),
                    "Packaged Admiral theme changes direct Radzen phone composition colors in dark mode");
            }

            // Act
            await directSelect.ClickAsync();

            var popup = page.Locator(".rz-dropdown-panel:visible").Last;

            // Assert
            await popup.WaitForAsync();

            // Act
            await popup.GetByText("Belarus", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
            await direct.GetByPlaceholder("Direct Radzen phone").FillAsync("+375 29 1234567");
            await direct.GetByPlaceholder("Direct Radzen phone").PressAsync("Tab");

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('[data-testid="phone-direct-model"]')
                    ?.textContent?.includes('Country: Belarus · Value: +375 29 1234567')
                """);

            Assert.That(await directSelect.GetCssAsync("visibility"), Is.EqualTo("visible"));
        }

        // Act & assert: exercise selection controls in both themes.
        foreach (var theme in new[]
        {
            "Light",
            "Dark"
        })
        {
            // Act
            await page.GotoAsync(consumerUrl + "/select-regression");

            // Assert
            await page.GetByRole(
                AriaRole.Heading,
                new PageGetByRoleOptions { Name = "Select package regression" }).WaitForAsync();
            await page.WaitForTimeoutAsync(800);

            // Act
            if (theme == "Dark")
            {
                await page.GetByTestId("select-theme-toggle").ClickAsync();
                await page.GetByTestId("select-theme").GetByText("Dark").WaitForAsync();
            }

            // Assert
            Assert.That(
                await page.GetByTestId("select-theme-link").GetAttributeAsync("href"),
                Does.Match(
                    (theme == "Dark") ? new Regex("admiral-dark-base\\.css$") : new Regex("admiral-base\\.css$")));

            var select = page.GetByTestId("select-admiral-single").Locator(".rz-dropdown");
            var selectLabel = page.GetByTestId("select-admiral-single").Locator(".admiral-input-field__label");

            // Assert
            var selectLabelId = await selectLabel.GetAttributeAsync("id");

            Assert.That(selectLabelId, Is.Not.Null.And.Not.Empty, $"{theme}: packaged SelectField label has an id");
            Assert.That(
                await select.GetAttributeAsync("aria-labelledby"),
                Is.EqualTo(selectLabelId),
                $"{theme}: packaged SelectField combobox uses the visible label as its accessible name");
            Assert.That(
                await page.GetByTestId("select-admiral-single").GetByRole(
                    AriaRole.Combobox,
                    new LocatorGetByRoleOptions { Name = "Package city" }).CountAsync(),
                Is.EqualTo(1),
                $"{theme}: packaged SelectField has one accessible combobox");

            // Act
            await selectLabel.ClickAsync();

            // Assert
            await page.WaitForFunctionAsync(
                "id => document.activeElement?.id === id",
                await select.GetAttributeAsync("id"));

            Assert.That(
                await select.EvaluateAsync<bool>("element => document.activeElement === element"),
                Is.EqualTo(true),
                $"{theme}: packaged SelectField label focuses the combobox");

            // Act
            await select.FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            var popup = page.Locator(".rz-dropdown-panel:visible").Last;

            // Assert
            await popup.WaitForAsync();

            // Act
            await popup.GetByText("Kazan", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('[data-testid="select-single-model"]')
                    ?.textContent?.includes('Value: Kazan')
                """);

            Assert.That(
                await page.GetByTestId("select-single-model").InnerTextAsync(),
                Does.Match(new Regex("Changes: 1")));
            Assert.That(
                await page.GetByTestId("select-callback-log").InnerTextAsync(),
                Is.EqualTo("single:Kazan"),
                $"{theme}: controlled single selection emits one callback after updating the value");
            // The model callback can finish before Radzen closes the portalled popup.
            await Assertions.Expect(popup).ToBeHiddenAsync();

            // Act
            await select.ClickAsync();

            // Assert
            await popup.WaitForAsync();

            // Act
            await popup.GetByText("Kazan", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

            // Assert
            await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            Assert.That(
                await page.GetByTestId("select-callback-log").InnerTextAsync(),
                Is.EqualTo("single:Kazan"),
                $"{theme}: reselecting the same value does not emit a callback");

            // Act
            await select.ClickAsync();

            // Assert
            await popup.WaitForAsync();

            // Act
            await popup.Locator("input.rz-dropdown-filter").FillAsync("Sochi");
            await page.WaitForTimeoutAsync(250);

            // Assert
            Assert.That(
                await page.GetByTestId("select-callback-log").InnerTextAsync(),
                Is.EqualTo("single:Kazan"),
                $"{theme}: filtering alone does not emit ValueChanged");

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            // Act
            await select.Locator(".rz-dropdown-clear-icon").ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('[data-testid="select-callback-log"]')
                    ?.textContent?.includes('single:<null>')
                """);

            Assert.That(
                await page.GetByTestId("select-callback-log").InnerTextAsync(),
                Is.EqualTo("single:Kazan | single:<null>"),
                $"{theme}: clear emits one null value after selection");

            var multiple = page.GetByTestId("select-admiral-multiple").Locator(".rz-dropdown");

            // Act
            await multiple.FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            var multiplePopup = page.Locator(".rz-multiselect-panel:visible").Last;

            // Assert
            await multiplePopup.WaitForAsync();

            // Act
            await multiplePopup.GetByText("Sochi", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

            // Assert
            await page.WaitForFunctionAsync(
                """
                () =>
                  document.querySelector('[data-testid="select-multiple-model"]')?.textContent?.includes('Sochi')
                """);

            Assert.That(
                await page.GetByTestId("select-multiple-model").InnerTextAsync(),
                Does.Match(new Regex("Changes: 1")),
                $"{theme}: multiple selection updates controlled values once");
            Assert.That(
                await page.GetByTestId("select-callback-log").InnerTextAsync(),
                Does.Match(new Regex("multiple:.*Sochi")),
                $"{theme}: multiple callback follows single select and clear events");

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await multiplePopup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            var direct = page.GetByTestId("select-direct-radzen").Locator(".rz-dropdown");

            // Act
            await direct.ClickAsync();

            // Assert
            await popup.WaitForAsync();

            // Act
            await popup.GetByText("Sochi", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('[data-testid="select-direct-model"]')
                    ?.textContent?.includes('Value: Sochi')
                """);

            Assert.That(
                (await direct.GetBoundsAsync())?.Width > 0,
                Is.True,
                $"{theme}: direct RadzenDropDown remains visible");
        }

        // Act
        TestContext.Progress.WriteLine(
            Convert.ToString(
                "Package consumer integration: PASS (assets, light/dark theme, AdmiralIcon, RadzenButton/TextBox, pagination callback, PhoneNumberInput, SelectField and direct RadzenDropDown)"));

        // Assert: no consumer asset requests failed.
        Assert.That(assetErrors, Is.Empty, "Consumer asset failures");
    }
}
