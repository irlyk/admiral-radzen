namespace Admiral.Playwright.Components.PhoneNumber;

[Category("PhoneNumber")]
public sealed class PhoneNumberTests : BrowserTest
{
    [TestCase("admiral", "ru", 1440, 900)]
    [TestCase("admiral", "ru", 390, 780)]
    [TestCase("admiral", "ru", 320, 360)]
    [TestCase("admiral", "en", 1440, 900)]
    [TestCase("admiral", "en", 390, 780)]
    [TestCase("admiral", "en", 320, 360)]
    [TestCase("admiral-dark", "ru", 1440, 900)]
    [TestCase("admiral-dark", "ru", 390, 780)]
    [TestCase("admiral-dark", "ru", 320, 360)]
    [TestCase("admiral-dark", "en", 1440, 900)]
    [TestCase("admiral-dark", "en", 390, 780)]
    [TestCase("admiral-dark", "en", 320, 360)]
    public async Task CountrySelectionAndPhoneEditingAsync(string theme, string culture, int width, int height)
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
        await page.GotoAsync(BaseUrl + "/components/phone-number-input");
        var phone = page.Locator(".admiral-phone-input").First;
        await phone.WaitForAsync();
        await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();
        var select = phone.Locator(".rz-dropdown");

        // Assert
        await Assertions.Expect(select).ToHaveCountAsync(1);

        var input = phone.Locator("input[inputmode=\"tel\"]");

        // Assert
        await Assertions.Expect(input).ToHaveCountAsync(1);

        // Assert: verify the initial label, flag and leading-content geometry.
        var leadingGeometry = await page.Locator(".admiral-phone-input").ReadSnapshotsAsync<PhoneInputGeometry>(
            """
            (elements) =>
              elements.map((element) => {
                const country = element.querySelector('.admiral-select')?.getBoundingClientRect();
                const field = element.querySelector('input[inputmode="tel"]');
                const box = field?.getBoundingClientRect();
                return {
                  size: element.className,
                  countryRight: country?.right,
                  textStart: box ? box.left + parseFloat(getComputedStyle(field).paddingInlineStart) : 0,
                };
              })
            """);

        // Assert
        foreach (var leading in leadingGeometry)
        {
            // Assert
            Assert.That(
                leading.TextStart >= leading.CountryRight + 2,
                Is.True,
                $"{theme}/{culture}/{viewport.width}: phone text must clear flag and chevron: {JsonSerializer.Serialize(leading)}");
        }

        // Assert
        Assert.That(
            await input.EvaluateAsync<bool>("element => element.labels?.length > 0"),
            Is.True,
            "Phone input has a programmatically associated label");

        var selectedFlag = select.Locator(".admiral-phone-input__country-value [data-admiral-icon-loaded] svg");

        // Assert
        await selectedFlag.WaitForAsync();
        var initialFlag = await selectedFlag.EvaluateAsync<string>("svg => svg.outerHTML");

        // Act: focus the country selector using the keyboard.
        await page.Keyboard.PressAsync("Tab");
        await select.FocusAsync();

        // Assert
        var focus = await select.ReadSnapshotAsync<KeyboardFocusStyle>("""
            (element) => {
              const style = getComputedStyle(element, '::before');
              return {
                visible: element.matches(':focus-visible'),
                border: style.borderTopStyle,
                width: style.borderTopWidth,
              };
            }
            """);

        Assert.That(
            focus,
            Is.EqualTo(new KeyboardFocusStyle(true, "solid", "2px")),
            "Country selector exposes a visible keyboard focus outline");

        // Act: open the country popup.
        await page.Keyboard.PressAsync("Enter");

        var popup = page.Locator(".rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)").First;

        // Assert
        await popup.WaitForAsync();
        await page.WaitForTimeoutAsync(300);

        // Assert: verify popup bounds and containment of its scrolling list.
        var geometry = await popup.ReadSnapshotAsync<PopupGeometry>(
            """
            (element) => {
              const panel = element.getBoundingClientRect();
              const list = element.querySelector('.rz-dropdown-items-wrapper')?.getBoundingClientRect();
              return {
                panel: panel.toJSON(),
                list: list?.toJSON(),
                viewport: { width: innerWidth, height: innerHeight },
                scrollHeight: element.querySelector('.rz-dropdown-items-wrapper')?.scrollHeight ?? 0,
              };
            }
            """);

        var panel = geometry.Panel;

        // Assert
        var list = geometry.List;

        Assert.That(panel.Width > 0 && panel.Height > 0, Is.True, "Popup has visible dimensions");
        Assert.That(
            panel.Left >= -1 && panel.Right <= viewport.width + 1 && panel.Top >= -1
                && panel.Bottom <= viewport.height + 1,
            Is.True,
            $"{theme}/{culture}/{viewport.width}: popup fits viewport: {JsonSerializer.Serialize(panel)}");
        Assert.That(
            list.Left >= panel.Left - 1 && list.Right <= panel.Right + 1 && list.Top >= panel.Top - 1
                && list.Bottom <= panel.Bottom + 1,
            Is.True,
            "Scrollable country list stays inside popup");
        Assert.That(
            await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"),
            Is.True,
            "Phone popup does not create horizontal document overflow");
        if (geometry.ScrollHeight > list.Height + 2)
        {
            var listElement = popup.Locator(".rz-dropdown-items-wrapper");

            // Assert
            await listElement.EvaluateAsync("element => { element.scrollTop = element.scrollHeight; }");

            Assert.That(
                await listElement.EvaluateAsync<bool>("element => element.scrollTop > 0"),
                Is.True,
                "Country list can scroll");
        }

        // Arrange: locate the popup search field.
        var search = popup.Locator("input").First;

        // Act: filter countries by name.
        await search.FillAsync("United");

        // Assert
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.rz-dropdown-panel.rz-open .rz-dropdown-item').length === 2");

        await Assertions.Expect(popup.Locator(".rz-dropdown-item")).ToHaveCountAsync(2);

        // Act: select a filtered country using the keyboard.
        await search.PressAsync("ArrowDown");

        // Assert
        await popup.Locator(".rz-dropdown-item.rz-state-highlight").First.WaitForAsync();
        var countryControlId = await select.GetAttributeAsync("id");

        // Act
        await search.PressAsync("Enter");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        await page.WaitForFunctionAsync(
            "() => /United/.test(document.querySelector('.admiral-phone-input .rz-dropdown')?.getAttribute('aria-label') ?? '')");

        // Assert: verify the new flag and stable selector identity.
        var flagName = (await select.GetAttributeAsync("aria-label") == "United Kingdom") ? "united-kingdom-of-great-britain-and-northern-ireland" : "united-states-of-america";
        await select.Locator($"[data-admiral-icon-loaded=\"flags/{flagName}\"] svg").WaitForAsync();

        Assert.That(
            await selectedFlag.EvaluateAsync<string>("svg => svg.outerHTML"),
            Is.Not.EqualTo(initialFlag),
            "Changing the country replaces the rendered SVG flag");
        Assert.That(
            await select.GetAttributeAsync("id"),
            Is.EqualTo(countryControlId),
            "Country selection preserves the combobox element");
        await page.WaitForFunctionAsync("id => document.activeElement?.id === id", countryControlId);

        // Act: dismiss a mouse-opened popup with Escape.
        await select.ClickAsync();

        // Assert
        await popup.WaitForAsync();

        // Act
        await page.Keyboard.PressAsync("Escape");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Act: leave a keyboard-opened popup with Tab.
        await select.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        // Assert
        await popup.WaitForAsync();

        // Act
        await page.Keyboard.PressAsync("Tab");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Act & assert: exercise popup placement and phone editing in the desktop layout.
        if (viewport.width == 1440)
        {
            // Assert
            await phone.EvaluateAsync("""
                (element) => {
                  element.style.position = 'fixed';
                  element.style.right = '4px';
                  element.style.top = '4px';
                  element.style.width = '240px';
                  element.style.zIndex = '1000';
                }
                """);

            // Act
            await select.ClickAsync();

            // Assert
            await popup.WaitForAsync();
            await page.WaitForTimeoutAsync(300);
            var edgeBox = await popup.GetBoundsAsync();

            Assert.That(
                edgeBox.X >= -1 && edgeBox.X + edgeBox.Width <= viewport.width + 1 && edgeBox.Y >= -1
                    && edgeBox.Y + edgeBox.Height <= viewport.height + 1,
                Is.True,
                "Country popup opened near the top/right viewport edge stays visible");

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            // Act
            await page.GotoAsync(BaseUrl + "/components/phone-number-input");

            // Assert
            await page.Locator(".admiral-consumer-shell[data-interactive=\"true\"]").WaitForAsync();

            var playground = page.Locator(".admiral-component-playground").First;
            var country = playground.Locator(".admiral-phone-input .rz-dropdown");

            // Act
            await country.ClickAsync();

            var countryPopup = page.Locator(
                ".rz-dropdown-panel:visible:has(.admiral-phone-input__country-option)").First;

            // Assert
            await countryPopup.WaitForAsync();

            // Act
            await countryPopup.GetByText("Казахстан", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

            // Assert
            await countryPopup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('.admiral-component-playground__preview [role=status]')
                    ?.textContent?.includes('KAZ')
                """);
            await page.WaitForTimeoutAsync(350);

            Assert.That(
                await playground.Locator("[role=status]").InnerTextAsync(),
                Does.Match(new Regex("KAZ")),
                "Country remains KAZ after country-selection callbacks settle");
            await country.Locator("[data-admiral-icon-loaded=\"flags/kazakhstan\"] svg").WaitForAsync();

            var number = playground.Locator("input[inputmode=\"tel\"]");

            // Act
            await number.FillAsync("+7 999 123-45-68");
            await number.PressAsync("Tab");

            // Assert
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('.admiral-component-playground__preview [role=status]')
                    ?.textContent?.includes('+7 999 123-45-68')
                """);

            Assert.That(
                await playground.Locator("[role=status]").InnerTextAsync(),
                Does.Match(new Regex("KAZ")),
                "Typing a shared +7 dial code keeps selected KAZ");

            // Act
            await playground.Locator(".admiral-component-controls select").Nth(2).SelectOptionAsync("Digits");

            // Assert
            await page.WaitForFunctionAsync(
                """
                () =>
                  document.querySelector('.admiral-component-playground input[inputmode="tel"]')?.value ===
                  '+7 999 123 45 68'
                """);

            // Act
            await country.ClickAsync();
            await countryPopup.GetByText("United Kingdom", new LocatorGetByTextOptions { Exact = true }).ClickAsync();

            // Assert
            await countryPopup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            await playground.Locator("[role=status]").GetByText(new Regex("449991234568.*GBR")).WaitForAsync();

            Assert.That(
                await number.InputValueAsync(),
                Is.EqualTo("+44 999 123 456 8"),
                "Digits mode replaces the dial code and preserves the national number");

            // Act
            await number.FillAsync("+375 29 123-45-67");

            // Assert
            await playground.Locator("[role=status]").GetByText(new Regex("375291234567.*BLR")).WaitForAsync();
            await page.WaitForFunctionAsync("""
                () =>
                  document
                    .querySelector('.admiral-component-playground .rz-dropdown')
                    ?.getAttribute('aria-label') === 'Беларусь'
                """);

            // Act & assert
            foreach (var size in new[]
            {
                "Small",
                "Medium",
                "ExtraLarge"
            })
            {
                // Act
                await playground.Locator(".admiral-component-controls select").First.SelectOptionAsync(size);

                var sizeName = size == "Small" ? "s" : size == "Medium" ? "m" : "xl";

                // Assert
                await playground.Locator($".admiral-phone-input--{sizeName}").WaitForAsync();

                // Act
                await page.Keyboard.PressAsync("Tab");
                await country.FocusAsync();

                // Assert
                await country.EvaluateAsync(
                    "element => Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished))");
                var bounds = await country.ReadSnapshotAsync<PhoneFocusRingBounds>(
                    """
                    (element) => {
                      const ring = getComputedStyle(element, '::before');
                      const box = element.getBoundingClientRect();
                      const input = element.closest('.admiral-phone-input').querySelector('input[inputmode="tel"]');
                      const field = input.getBoundingClientRect();
                      const height = parseFloat(ring.height);
                      return {
                        top: box.top + parseFloat(ring.top) - height / 2,
                        bottom: box.top + parseFloat(ring.top) + height / 2,
                        right: box.left + parseFloat(ring.left) + parseFloat(ring.width),
                        fieldTop: field.top,
                        fieldBottom: field.bottom,
                        textStart: field.left + parseFloat(getComputedStyle(input).paddingInlineStart),
                      };
                    }
                    """);

                Assert.That(
                    bounds.Top > bounds.FieldTop && bounds.Bottom < bounds.FieldBottom
                        && bounds.Right < bounds.TextStart,
                    Is.True,
                    $"Country focus stays inside the field and clear of the number in {size}: {JsonSerializer.Serialize(bounds)}");
            }

            // Act
            await playground.GetByRole(
                AriaRole.Checkbox,
                new LocatorGetByRoleOptions { Name = "Disabled" }).CheckAsync();

            // Assert
            await page.WaitForFunctionAsync(
                "() => document.querySelector('.admiral-component-playground__preview input[inputmode=\"tel\"]')?.disabled === true");

            Assert.That(await number.IsDisabledAsync(), Is.EqualTo(true), "Disabled phone cannot be edited");
            Assert.That(await country.EvaluateAsync<bool>("""
                (element) =>
                  element.classList.contains('rz-state-disabled') ||
                  element.getAttribute('aria-disabled') === 'true'
                """), Is.True, "Disabled country selector cannot open");

            // Act
            await playground.GetByRole(
                AriaRole.Checkbox,
                new LocatorGetByRoleOptions { Name = "Disabled" }).UncheckAsync();

            // Assert
            await page.WaitForFunctionAsync(
                "() => document.querySelector('.admiral-component-playground__preview input[inputmode=\"tel\"]')?.disabled === false");

            // Act
            await playground.Locator(".admiral-component-controls input[type=checkbox]").Nth(4).CheckAsync();

            // Assert
            await page.WaitForFunctionAsync(
                "() => document.querySelector('.admiral-component-playground__preview input[inputmode=\"tel\"]')?.readOnly === true");

            Assert.That(
                await number.GetAttributeAsync("readonly"),
                Is.EqualTo(""),
                "Read-only phone has readonly input");
        }

        await Assertions.Expect(page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
    }
}
