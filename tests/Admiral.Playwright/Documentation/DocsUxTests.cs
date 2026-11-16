using Admiral.Playwright.Pages.Documentation;

namespace Admiral.Playwright.Documentation;

[Category("DocsUx")]
public sealed class DocsUxTests : GalleryTest
{
    [TestCase("admiral", "ru", TestName = "DocsUx: Home entry and category navigation / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: Home entry and category navigation / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: Home entry and category navigation / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: Home entry and category navigation / admiral-dark / en")]
    public async Task HomeEntryAndCategoryNavigationAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();
        await documentation.NavigateAsync("/");

        // Assert
        await Assertions.Expect(Page.Locator(".docs-home")).ToHaveCountAsync(1);
        await Assertions.Expect(Page.Locator(".docs-home__category")).ToHaveCountAsync(6);

        var headings = (language == "ru") ? new[]
        {
            "Основы",
            "Действия",
            "Поля ввода",
            "Навигация",
            "Отображение данных",
            "Обратная связь"
        }
        : new[]
        {
            "Basics",
            "Actions",
            "Inputs",
            "Navigation",
            "Data Display",
            "Feedback"
        };

        // Assert
        Assert.That(await Page.Locator(".docs-home__category h2").AllInnerTextsAsync(), Is.EqualTo(headings));

        var sidebar = Page.Locator(".admiral-side-menu");

        // Assert
        foreach (var name in headings)
        {
            // Assert
            await sidebar.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = name, Exact = true }).WaitForAsync();
        }

        var start = Page.Locator(".docs-home__hero-links a[href=\"/components/button\"]");

        // Act
        await start.FocusAsync();
        await start.PressAsync("Enter");
        await Page.WaitForURLAsync("**/components/button");
        await documentation.NavigateAsync("/");
        await Page.Locator(".docs-home a[href=\"/regression/overview\"]").ClickAsync();
        await Page.WaitForURLAsync("**/regression/overview");

        // Assert
        await Page.Locator("#binary-heading").WaitForAsync();

        // Act
        await documentation.NavigateAsync("/");
        await Page.Locator(".docs-home__directory a[href=\"/guide/integration\"]").ClickAsync();
        await Page.WaitForURLAsync("**/guide/integration");

        // Assert
        await Page.Locator(".docs-code-example").First.WaitForAsync();
        await Page.Locator(".component-doc-api").First.WaitForAsync();
    }

    [TestCase("admiral", "ru", TestName = "DocsUx: Theme switch has an accessible name and toggles both ways / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: Theme switch has an accessible name and toggles both ways / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: Theme switch has an accessible name and toggles both ways / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: Theme switch has an accessible name and toggles both ways / admiral-dark / en")]
    public async Task ThemeSwitchHasAnAccessibleNameAndTogglesBothWaysAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();
        await documentation.NavigateAsync("/");
        var toggle = Page.Locator(".admiral-layout__header .rz-toggle-button");
        var name = (language == "ru") ? "Переключить цветовую тему Admiral" : "Switch Admiral color theme";

        // Assert
        Assert.That(await toggle.GetAttributeAsync("aria-label"), Is.EqualTo(name));

        var originalState = await toggle.GetAttributeAsync("aria-pressed");

        // Act
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "previous => document.querySelector('.admiral-layout__header .rz-toggle-button')?.getAttribute('aria-pressed') !== previous",
            originalState);

        // Act
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "previous => document.querySelector('.admiral-layout__header .rz-toggle-button')?.getAttribute('aria-pressed') === previous",
            originalState);
    }

    [TestCase("admiral", "ru", TestName = "DocsUx: Sidebar search handles local names and keyboard clear / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: Sidebar search handles local names and keyboard clear / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: Sidebar search handles local names and keyboard clear / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: Sidebar search handles local names and keyboard clear / admiral-dark / en")]
    public async Task SidebarSearchHandlesLocalNamesAndKeyboardClearAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();
        await documentation.NavigateAsync("/");
        var search = Page.GetByTestId("component-search");

        // Act
        await search.FillAsync((language == "ru") ? "флажок" : "Checkbox");

        var sidebar = Page.Locator(".admiral-side-menu");
        var checkbox = sidebar.GetByRole(
            AriaRole.Link,
            new LocatorGetByRoleOptions { Name = "Checkbox", Exact = true });

        // Assert
        await checkbox.WaitForAsync();

        // Act
        await Page.GetByTestId("component-search-clear").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"component-search\"]')?.value === ''");

        Assert.That(
            await search.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(true));

        // Act
        await search.FillAsync("NoSuchComponent987");

        // Assert
        await Page.GetByTestId("component-search-empty").WaitForAsync();

        // Act
        await Page.GetByTestId("component-search-clear").ClickAsync();
        await search.FillAsync("Button");

        var button = sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Button", Exact = true });

        // Act
        await button.FocusAsync();
        await button.PressAsync("Enter");
        await Page.WaitForURLAsync("**/components/button");
    }

    [TestCase("admiral", "ru", TestName = "DocsUx: Pilot tabs, live callbacks and copyable Razor / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: Pilot tabs, live callbacks and copyable Razor / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: Pilot tabs, live callbacks and copyable Razor / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: Pilot tabs, live callbacks and copyable Razor / admiral-dark / en")]
    public async Task PilotTabsLiveCallbacksAndCopyableRazorAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();

        // Act & assert
        foreach (var (route, kind) in new[]
        {
            ("/components/button", "button"),
            ("/components/checkbox", "checkbox"),
            ("/tables", "table")
        })
        {
            // Act
            await documentation.NavigateAsync(route);
            await Page.Locator(".component-doc-tabs [data-doc-tab=\"admiral\"]").ClickAsync();

            // Assert
            if (kind == "button")
            {
                var preview = Page.Locator(".admiral-component-playground__preview").First;

                // Act
                await preview.GetByRole(AriaRole.Button).First.ClickAsync();

                // Assert
                await Page.WaitForFunctionAsync(
                    "() => /:\\s*1\\b/.test(document.querySelector('.admiral-component-playground__preview .admiral-component-page__result')?.textContent ?? '')");
            }
            else if (kind == "checkbox")
            {
                var result = Page.GetByTestId("checkbox-admiral-result");
                await Page.GetByTestId("checkbox-admiral-demo").GetByRole(AriaRole.Checkbox).First.ClickAsync();
                await Page.WaitForFunctionAsync(
                    "() => document.querySelector('[data-testid=\"checkbox-admiral-result\"]')?.textContent?.includes('1')");

                await Assertions.Expect(result).ToBeVisibleAsync();
            }
            else
            {
                var review = Page.Locator(
                    ".admiral-table-playground__table .admiral-data-grid__row-actions button").First;
                await review.ClickAsync();
                await Page.WaitForFunctionAsync(
                    "() => /Review|Проверка/.test(document.querySelector('.admiral-table-playground__preview .admiral-component-page__result')?.textContent ?? '')");
            }

            // Act
            await Page.Locator(".docs-code-example").First.GetByRole(AriaRole.Button).ClickAsync();

            // Assert
            await Page.WaitForFunctionAsync(
                "() => /copied|скопирован/i.test(document.querySelector('.docs-code-example [role=\"status\"]')?.textContent ?? '')");
            var copied = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

            Assert.That(copied, Does.Match(new Regex("<(Admiral|Radzen)")));

            // Act
            await Page.Locator(".component-doc-tabs [data-doc-tab=\"api\"]").ClickAsync();

            // Assert
            await Page.Locator(".docs-code-example").First.WaitForAsync();

            // Act
            await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

            var direct = Page.Locator(".admiral-component-playground__preview").First;

            // Act
            if (kind == "button")
            {
                await direct.GetByRole(AriaRole.Button).First.ClickAsync();
                await Page.WaitForFunctionAsync(
                    "() => /\\b1\\b/.test(document.querySelector('.admiral-component-playground__preview [role=\"status\"]')?.textContent ?? '')");
            }
            else if (kind == "checkbox")
            {
                await Page.GetByTestId("checkbox-radzen-demo").GetByRole(AriaRole.Checkbox).First.ClickAsync();
                await Page.WaitForFunctionAsync(
                    "() => document.querySelector('[data-testid=\"checkbox-radzen-result\"]')?.textContent?.includes('1')");
            }
            else
            {
                var status = direct.GetByRole(AriaRole.Status);
                var before = await status.InnerTextAsync();
                await direct.Locator(".rz-data-grid-data tbody tr").First.ClickAsync();
                await Page.WaitForFunctionAsync("""
                    (previous) => {
                      const current = document
                        .querySelector('.admiral-component-playground__preview [role="status"]')
                        ?.textContent?.trim();
                      return current && current !== previous.trim();
                    }
                    """, before);
            }

            await Page.Locator(".docs-code-example").First.GetByRole(AriaRole.Button).ClickAsync();

            // Assert
            await Page.WaitForFunctionAsync(
                "() => /copied|скопирован/i.test(document.querySelector('.docs-code-example [role=\"status\"]')?.textContent ?? '')");
        }
    }

    [TestCase("admiral", "ru", TestName = "DocsUx: Mobile docs navigation and page overflow / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: Mobile docs navigation and page overflow / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: Mobile docs navigation and page overflow / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: Mobile docs navigation and page overflow / admiral-dark / en")]
    public async Task MobileDocsNavigationAndPageOverflowAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();
        await Page.SetViewportSizeAsync(390, 844);

        // Act & assert
        foreach (var route in new[]
        {
            "/",
            "/components/button",
            "/components/checkbox",
            "/tables"
        })
        {
            // Act
            await documentation.NavigateAsync(route);

            // Assert
            var overflow = await Page.EvaluateAsync<double>("() => document.documentElement.scrollWidth - innerWidth");

            Assert.That(
                overflow <= 1,
                Is.True,
                $"{route} does not overflow the 390px viewport (overflow {overflow}px)");
        }

        // Act
        await documentation.NavigateAsync("/");
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { NameRegex = new Regex("Open navigation|Открыть навигацию") }).ClickAsync();

        var search = Page.GetByTestId("component-search");

        // Assert
        await search.WaitForAsync();

        // Act
        await search.FillAsync("Button");

        // Assert
        await Page.Locator(".admiral-side-menu").GetByRole(
            AriaRole.Link,
            new LocatorGetByRoleOptions { Name = "Button", Exact = true }).WaitForAsync();

        // Act
        await Page.SetViewportSizeAsync(1440, 900);
    }

    [TestCase("admiral", "ru", TestName = "DocsUx: Dedicated family pages expose Admiral, API and direct alternatives / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: Dedicated family pages expose Admiral, API and direct alternatives / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: Dedicated family pages expose Admiral, API and direct alternatives / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: Dedicated family pages expose Admiral, API and direct alternatives / admiral-dark / en")]
    public async Task DedicatedFamilyPagesExposeAdmiralAPIAndDirectAlternativesAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();
        var routes = new (string Route, string Admiral, string? Radzen)[]
        {
            ("/components/input-field", "input-field-admiral-demo", "input-field-radzen-demo"),
            ("/components/drop-down", "drop-down-admiral-demo", "drop-down-radzen-demo"),
            ("/components/date-range-picker", "date-range-admiral-demo", "date-range-radzen-demo"),
            ("/components/avatar", "avatar-admiral-demo", null),
            ("/components/global-search", "global-search-admiral-demo", null),
            ("/components/grid", "grid-admiral-demo", "grid-radzen-demo"),
            ("/components/pagination", "pagination-admiral-demo", "pagination-radzen-demo"),
            ("/components/progress", "progress-admiral-demo", null),
            ("/components/spinner", "spinner-admiral-demo", null),
            ("/components/status-indicator", "status-indicator-admiral-demo", null)
        };

        // Act & assert
        foreach (var (route, admiralDemo, radzenDemo) in routes)
        {
            // Act
            await documentation.NavigateAsync(route);

            // Assert
            await Page.GetByTestId(admiralDemo).WaitForAsync();

            // Act
            await Page.Locator(".component-doc-tabs [data-doc-tab=\"api\"]").ClickAsync();

            // Assert
            await Page.Locator(".docs-code-example").First.WaitForAsync();

            // Act
            await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

            // Assert
            if (radzenDemo != null)
            {
                await Page.GetByTestId(radzenDemo).WaitForAsync();

                Assert.That(
                    await Page.GetByTestId(radzenDemo).Locator("[class^=\"rz-\"], [class*=\" rz-\"]").CountAsync() > 0,
                    Is.EqualTo(true),
                    $"{route} renders a direct Radzen control");
            }
            else
            {
                await Page.Locator(".admiral-component-playground__preview").First.WaitForAsync();
            }
        }
    }

    [TestCase("admiral", "ru", TestName = "DocsUx: PaginationOne full and simple modes keep page callbacks / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsUx: PaginationOne full and simple modes keep page callbacks / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsUx: PaginationOne full and simple modes keep page callbacks / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsUx: PaginationOne full and simple modes keep page callbacks / admiral-dark / en")]
    public async Task PaginationOneFullAndSimpleModesKeepPageCallbacksAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.EnableClipboardAsync();
        await documentation.NavigateAsync("/components/pagination");
        var demo = Page.GetByTestId("pagination-admiral-demo");
        var pager = demo.Locator(".admiral-pagination-one");
        var nextPageLabel = (language == "ru") ? "Следующая страница" : "Next page";

        // Assert
        await Assertions.Expect(pager.Locator(".admiral-pagination-one__page-size")).ToHaveCountAsync(1);

        // Act
        await pager.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = nextPageLabel }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"pagination-admiral-demo\"] [role=\"status\"]')?.textContent?.includes('2')");

        // Act
        await demo.GetByRole(AriaRole.Checkbox, new LocatorGetByRoleOptions { Name = "Simple" }).CheckAsync();

        // Assert
        await pager.Locator(".admiral-pagination-one__page-size").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        await Assertions.Expect(
            pager.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = nextPageLabel })).ToHaveCountAsync(1);

        // Act
        await Page.SetViewportSizeAsync(390, 844);
        await documentation.NavigateAsync("/components/pagination");

        var preview = Page.GetByTestId("pagination-admiral-demo").Locator(".pagination-doc__preview");

        // Assert
        var geometry = await preview.ReadSnapshotAsync<ScrollViewportSnapshot>(
            "element => ({ viewport: element.clientWidth, content: element.scrollWidth, left: element.getBoundingClientRect().left })");

        Assert.That(
            geometry.Content > geometry.Viewport,
            Is.True,
            "full mode has an explicit internal horizontal scroll area on mobile");

        // Act
        await preview.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"pagination-admiral-demo\"] .pagination-doc__preview')?.scrollLeft > 0");

        // Act
        await Page.SetViewportSizeAsync(1440, 900);
    }
}
