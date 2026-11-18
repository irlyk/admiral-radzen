namespace Admiral.Playwright.Integration.Wasm;

[Category("Wasm")]
public sealed class WasmTests : BrowserTest
{
    [TestCase(1280, 850)]
    [TestCase(390, 800)]
    public async Task ExamplesWorkUnderStaticProjectSubpathAsync(int width, int height)
    {
        // Arrange: publish the static WASM site and monitor subpath asset requests.
        var host = Own(await AppHost.StartWasmAsync());
        var url = host.Url;
        var routes = new[]
        {
            "/",
            "/components/button",
            "/components/button-groups",
            "/components/icon-button-group",
            "/components/multi-button",
            "/components/text-button-menu",
            "/components/floating-button",
            "/components/floating-button-menu",
            "/components/checkbox",
            "/components/select",
            "/components/phone-number-input",
            "/tables",
            "/components/tooltip",
            "/basics/colors",
            "/guide/getting-started",
            "/guide/integration",
            "/components/typography",
            "/components/switch"
        };
        var viewport = new
        {
            width,
            height
        };
        var (context, page) = await NewPageAsync(width: width, height: height, url: url);
        page.SetDefaultTimeout(20000);
        var consoleErrors = new HashSet<string>();
        var missing = new HashSet<string>();
        var rootAssets = new HashSet<string>();
        var fixtureResponses = new HashSet<string>();
        const string prefix = "/admiral-radzen/";
        page.Console += (_, message) =>
        {
            if (message.Type != "error")
            {
                return;
            }

            var source = message.Location;
            if (Regex.IsMatch(message.Text, "^Failed to load resource:.*404"))
            {
                return;
            }

            consoleErrors.Add(message.Text + " @ " + source);
        };
        page.Request += (_, request) =>
        {
            var path = new Uri(request.Url).AbsolutePath;
            if (path.StartsWith("/_content/") || path.StartsWith("/_framework/"))
            {
                rootAssets.Add(path);
            }
        };
        page.Response += (_, response) =>
        {
            var path = new Uri(response.Url).AbsolutePath;
            if (path.StartsWith(prefix + "css/fonts/") && response!.Status == 200)
            {
                fixtureResponses.Add(path);
            }

            if (response!.Status >= 400
                && !(response!.Status == 404 && routes.Skip(1).Any(route => path == prefix.TrimEnd('/') + route)))
            {
                missing.Add($"{response!.Status} {path}");
            }
        };
        var fontFixtureDir = Environment.GetEnvironmentVariable("ADMIRAL_FONT_FIXTURE_DIR");

        // Act: open the statically hosted application.
        var response = await page.GotoAsync(url);

        // Assert: the index loads with the default language and sufficient contrast.
        Assert.That(response!.Status, Is.EqualTo(200), "Index must return 200");

        await page.GetByRole(
            AriaRole.Heading,
            new PageGetByRoleOptions { Name = "Документация компонентов" }).WaitForAsync();

        // Assert
        Assert.That(await page.Locator("html").GetAttributeAsync("lang"), Is.EqualTo("ru"), "RU is default");

        var lightBackground = await page.Locator(".docs-shell").GetCssAsync("background-color");
        var homeLink = page.Locator("#main-content a").First;
        var lightLinkColor = await homeLink.GetCssAsync("color");

        // Assert
        Assert.That(
            ColorContrast.Ratio(lightLinkColor, lightBackground) >= 4.5,
            Is.True,
            $"Light home link contrast: {lightLinkColor} on {lightBackground}");

        // Act: switch documentation to English.
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "EN", Exact = true }).ClickAsync();

        // Assert
        await page.GetByRole(
            AriaRole.Heading,
            new PageGetByRoleOptions { Name = "Component documentation" }).WaitForAsync();

        Assert.That(
            await page.EvaluateAsync<string>("() => localStorage.getItem('admiral-docs-culture')"),
            Is.EqualTo("en"));

        // Act: switch the static application to the dark theme.
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Switch theme" }).ClickAsync();

        // Assert
        await page.WaitForFunctionAsync("""
            () => document.getElementById('admiral-theme')?.href.includes('/admiral-dark-base.css')
            """);

        Assert.That(
            await page.EvaluateAsync<string>("() => localStorage.getItem('admiral-docs-theme')"),
            Is.EqualTo("admiral-dark"));
        await page.WaitForFunctionAsync(
            """
            (previous) => getComputedStyle(document.querySelector('.docs-shell')).backgroundColor !== previous
            """,
            lightBackground);
        var darkBackground = await page.Locator(".docs-shell").GetCssAsync("background-color");
        var darkLinkColor = await homeLink.GetCssAsync("color");

        Assert.That(
            ColorContrast.Ratio(darkLinkColor, darkBackground) >= 4.5,
            Is.True,
            $"Dark home link contrast: {darkLinkColor} on {darkBackground}");

        // Act & assert: open and reload deep links through the genuine 404 fallback.
        foreach (var route in routes[1..])
        {
            // Act
            response = await page.GotoAsync(url[..^1] + route);

            // Assert
            Assert.That(response!.Status, Is.EqualTo(404), $"{route}: static fallback uses 404.html");
            await page.Locator("h1").First.WaitForAsync();

            // Act
            await page.ReloadAsync();

            // Assert
            await page.Locator("h1").First.WaitForAsync();
            var content = (route == "/basics/colors") ? "[data-color-token=\"--rz-primary\"]" : ((route == "/guide/integration") ? ".docs-code-example" : ".admiral-component-playground");

            Assert.That(
                await page.Locator(content).CountAsync() > 0,
                Is.True,
                $"{route}: shared documentation content renders");
            Assert.That(
                await page.EvaluateAsync<string>("() => localStorage.getItem('admiral-docs-culture')"),
                Is.EqualTo("en"));
            Assert.That(
                await page.Locator("#admiral-theme").GetAttributeAsync("href"),
                Does.Match(new Regex("admiral-dark-base\\.css")));
        }

        // Act: exercise the shared switch example.
        await page.GotoAsync(url + "components/switch");

        var switchDemo = page.GetByTestId("switch-playground");

        // Act
        await switchDemo.Locator("label.rz-label").ClickAsync();

        // Assert
        await page.WaitForFunctionAsync("() => document.getElementById('switch-playground')?.checked === true");

        // Act
        await switchDemo.GetByRole(
            AriaRole.Checkbox,
            new LocatorGetByRoleOptions { Name = "ReadOnly", Exact = true }).CheckAsync();

        // Assert
        await page.WaitForFunctionAsync("() => document.getElementById('switch-playground')?.disabled === true");

        Assert.That(await switchDemo.GetByRole(AriaRole.Status).InnerTextAsync(), Does.Match(new Regex("true.*1")));

        // Act: invoke a button callback in WASM.
        await page.GotoAsync(url + "components/button");

        var button = page.Locator(".admiral-component-playground__preview button").First;

        // Act
        await button.ClickAsync();

        // Assert
        Assert.That(
            await page.Locator(".admiral-component-playground__preview").First.InnerTextAsync(),
            Does.Match(new Regex("Click count:\\s*1")));

        // Act: follow a related-component link.
        await page.GotoAsync(url + "components/checkbox");

        var relatedLink = page.Locator("#main-content").GetByRole(
            AriaRole.Link,
            new LocatorGetByRoleOptions { Name = "Checkbox groups" });

        // Assert
        await relatedLink.WaitForAsync();

        // Assert: related links retain the project subpath.
        Assert.That(
            new Uri(new Uri(url), await relatedLink.GetAttributeAsync("href")).AbsolutePath,
            Is.EqualTo("/admiral-radzen/components/checkbox-group"),
            "Shared page link stays in project subpath");

        // Act
        await relatedLink.ClickAsync();

        // Assert
        await page.Locator("#main-content").GetByRole(
            AriaRole.Heading,
            new LocatorGetByRoleOptions { Name = "Checkbox groups", Exact = true }).WaitForAsync();

        // Shared group links must resolve under the static site's project prefix.
        await page.GotoAsync(url + "components/button-groups");
        var iconGroups = page.Locator("#main-content").GetByRole(AriaRole.Link,
            new() { Name = "IconButtonGroup has its own page", Exact = true });
        Assert.That(new Uri(new Uri(url), await iconGroups.GetAttributeAsync("href")).AbsolutePath,
            Is.EqualTo("/admiral-radzen/components/icon-button-group"));
        await iconGroups.ClickAsync();
        await page.Locator("#main-content").GetByRole(AriaRole.Heading,
            new() { Name = "IconButtonGroup", Exact = true }).WaitForAsync();
        var textGroups = page.Locator("#main-content").GetByRole(AriaRole.Link,
            new() { Name = "ButtonGroup page", Exact = true });
        Assert.That(new Uri(new Uri(url), await textGroups.GetAttributeAsync("href")).AbsolutePath,
            Is.EqualTo("/admiral-radzen/components/button-groups"));
        await textGroups.ClickAsync();
        await page.Locator("[data-doc-tab=theme]").ClickAsync();
        var groupPreview = page.Locator(".admiral-component-playground__preview").First;
        var directGroup = groupPreview.GetByRole(AriaRole.Group, new() { Name = "Document actions", Exact = true });
        await directGroup.GetByRole(AriaRole.Button).First.ClickAsync();
        await Assertions.Expect(groupPreview.Locator("output")).ToHaveTextAsync(new Regex(@"Clicks:\s*1$"));
        var disableLast = page.GetByLabel("Disabled · last", new() { Exact = true });
        await disableLast.UncheckAsync();
        await Assertions.Expect(directGroup.GetByRole(AriaRole.Button).Last).ToBeEnabledAsync();
        await disableLast.CheckAsync();
        await Assertions.Expect(directGroup.GetByRole(AriaRole.Button).Last).ToBeDisabledAsync();
        await Assertions.Expect(directGroup.GetByRole(AriaRole.Button).Last).ToHaveCSSAsync("color", "rgb(81, 83, 87)");

        // Floating action links and both implementations work under the project prefix.
        await page.GotoAsync(url + "components/floating-button");
        var floatingMenuLink = page.Locator("#main-content a[href='components/floating-button-menu']");
        Assert.That(new Uri(new Uri(url), await floatingMenuLink.GetAttributeAsync("href")).AbsolutePath,
            Is.EqualTo("/admiral-radzen/components/floating-button-menu"));
        await floatingMenuLink.ClickAsync();
        var floatingTrigger = page.Locator(".admiral-floating-button-menu").First.Locator(":scope > button");
        await floatingTrigger.ClickAsync();
        await Assertions.Expect(floatingTrigger.Locator("[data-admiral-icon-loaded='service/close-outline'] svg")).ToBeVisibleAsync();
        await floatingTrigger.ClickAsync();
        await Assertions.Expect(floatingTrigger).ToHaveAttributeAsync("aria-expanded", "false");
        await page.Locator("#main-content a[href='components/floating-button']").ClickAsync();
        await page.Locator("[data-doc-tab=theme]").ClickAsync();
        var floatingDirect = page.Locator(".admiral-component-playground__preview .rz-button.admiral-floating-button");
        await floatingDirect.ClickAsync();
        await Assertions.Expect(page.Locator(".admiral-component-playground__preview output")).ToHaveTextAsync(new Regex(@"Clicks:\s*1$"));
        await page.GetByLabel("Disabled", new() { Exact = true }).CheckAsync();
        await Assertions.Expect(floatingDirect).ToBeDisabledAsync();
        await Assertions.Expect(floatingDirect.Locator(".admiral-floating-button__content")).ToHaveCSSAsync("color", "rgb(81, 83, 87)");

        // The three menu-action pages share working examples with the package consumer.
        foreach (var route in new[] { "multi-button", "text-button-menu" })
        {
            await page.GotoAsync(url + "components/" + route);
            var demo = page.GetByTestId("menu-action-playground");
            var trigger = demo.Locator(".admiral-menu__trigger button");
            await trigger.ClickAsync();
            var menu = page.Locator(".admiral-menu__popup");
            await menu.GetByRole(AriaRole.Menuitem, new() { Name = "Print", Exact = true }).ClickAsync();
            await Assertions.Expect(menu).ToHaveCountAsync(0);
            await Assertions.Expect(demo.Locator("output")).ToContainTextAsync("Actions: 1 · Last: print");
            await page.Locator("[data-doc-tab=theme]").ClickAsync();
            await demo.Locator(".rz-splitbutton-menubutton").ClickAsync();
            var native = page.Locator(".rz-splitbutton-menu:visible");
            await native.GetByRole(AriaRole.Menuitem, new() { Name = "Print", Exact = true }).ClickAsync();
            await Assertions.Expect(demo.Locator("output")).ToContainTextAsync("Actions: 1 · Last: print");
            await page.GetByLabel("Disabled", new() { Exact = true }).CheckAsync();
            await Assertions.Expect(demo.Locator(".rz-splitbutton-menubutton")).ToBeDisabledAsync();
            var related = page.Locator("#main-content a[href='components/menu']");
            Assert.That(new Uri(new Uri(url), await related.GetAttributeAsync("href")).AbsolutePath,
                Is.EqualTo("/admiral-radzen/components/menu"));
        }

        // Act: navigate through the searchable component catalog.
        var catalogToggle = page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Catalog", Exact = true });

        // Act
        if (await catalogToggle.IsVisibleAsync())
        {
            await catalogToggle.ClickAsync();
        }

        await page.GetByRole(
            AriaRole.Searchbox,
            new PageGetByRoleOptions { Name = "Search components" }).FillAsync("slider");

        var sliderLink = page.Locator(".docs-nav a[href=\"components/slider\"]");

        // Act
        await sliderLink.ClickAsync();

        // Assert
        await page.Locator("#main-content").GetByRole(
            AriaRole.Heading,
            new LocatorGetByRoleOptions { Level = 1, Name = "Slider / Range" }).WaitForAsync();

        Assert.That(await sliderLink.GetAttributeAsync("aria-current"), Is.EqualTo("page"));
        if (await catalogToggle.IsVisibleAsync())
        {
            Assert.That(await catalogToggle.GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));
        }

        // Act: exercise the independent direct Radzen checkbox.
        await page.GotoAsync(url + "components/checkbox");
        await page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

        var directCheckbox = page.Locator("[data-testid=\"checkbox-radzen-demo\"] .rz-chkbox").First;

        // Act
        await directCheckbox.ClickAsync();

        // Assert
        Assert.That(
            await page.Locator("[data-testid=\"checkbox-radzen-result\"]").InnerTextAsync(),
            Does.Match(new Regex("Changes:\\s*1")),
            "Direct Radzen checkbox emits an observable callback");

        // Act: open a selection popup in WASM.
        await page.GotoAsync(url + "components/select");

        var combo = page.Locator(".admiral-component-playground [role=\"combobox\"]").First;

        // Act
        await combo.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        var popup = page.Locator(".rz-dropdown-panel:visible").Last;

        // Assert
        await popup.WaitForAsync();
        var box = await popup.GetBoundsAsync();

        Assert.That(
            box.Width > 0 && box.Height > 0 && box.X >= -1 && box.X + box.Width <= viewport.width + 1,
            Is.True,
            "Select popup fits horizontal viewport");

        // Act
        await page.Keyboard.PressAsync("Escape");

        // Assert
        await popup.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        // Act & assert: load optional consumer fonts from the project subpath.
        if (!string.IsNullOrEmpty(fontFixtureDir))
        {
            // Act
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = url + "css/admiral-font-opt-in.css" });

            // Assert
            await page.EvaluateAsync("""
                async () => {
                  for (const weight of [400, 500, 550])
                    await document.fonts.load(`${weight} 16px "Admiral WASM Fixture"`);
                }
                """);

            Assert.That(
                await page.Locator("body").GetCssAsync("font-family"),
                Does.Match(new Regex("Admiral WASM Fixture")),
                "WASM body uses consumer font");
            Assert.That(
                await combo.GetCssAsync("font-family"),
                Does.Match(new Regex("Admiral WASM Fixture")),
                "WASM combobox uses consumer font");

            // Act
            await combo.FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            // Assert
            await popup.WaitForAsync();

            Assert.That(
                await popup.GetCssAsync("font-family"),
                Does.Match(new Regex("Admiral WASM Fixture")),
                "WASM popup uses consumer font");

            // Act
            await page.Keyboard.PressAsync("Escape");

            // Assert
            Assert.That(fixtureResponses.Count, Is.EqualTo(3), "All three consumer font weights load over HTTP");
        }

        // Act: change the theme and reload the application.
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Switch theme" }).ClickAsync();
        await page.ReloadAsync();

        // Assert: theme persistence and all asset requests respect static hosting.
        Assert.That(
            await page.Locator("#admiral-theme").GetAttributeAsync("href"),
            Does.Match(new Regex("admiral-base\\.css$")));
        Assert.That(consoleErrors, Is.EqualTo(Array.Empty<object>()), $"Browser console errors at {viewport.width}");
        Assert.That(missing, Is.EqualTo(Array.Empty<object>()), $"Unexpected asset failures at {viewport.width}");
        Assert.That(
            rootAssets,
            Is.EqualTo(Array.Empty<object>()),
            $"Package assets escaped project subpath at {viewport.width}");

        // Act
        TestContext.Progress.WriteLine(Convert.ToString($"WASM static host {viewport.width}px: PASS"));
    }
}
