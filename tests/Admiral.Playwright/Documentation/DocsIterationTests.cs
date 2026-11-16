using Admiral.Playwright.Pages.Documentation;

namespace Admiral.Playwright.Documentation;

[Category("DocsIteration")]
public sealed class DocsIterationTests : GalleryTest
{
    [TestCase("admiral", "ru", TestName = "DocsIteration: Theme survives language navigation and reload / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsIteration: Theme survives language navigation and reload / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsIteration: Theme survives language navigation and reload / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsIteration: Theme survives language navigation and reload / admiral-dark / en")]
    public async Task ThemeSurvivesLanguageNavigationAndReloadAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        var initialTheme = (theme == "admiral") ? "admiral-dark" : "admiral";
        await Context.AddCookiesAsync([new() { Name = "RadzenAdmiralTheme", Value = initialTheme, Url = BaseUrl }]);
        await documentation.NavigateAsync("/components/button");
        var brand = Page.Locator(".admiral-consumer-brand");
        await brand.GetByText(new Regex(initialTheme)).WaitForAsync();

        // Act
        await Page.Locator(".admiral-layout__header .rz-toggle-button").ClickAsync();

        // Assert
        await brand.GetByText(new Regex(theme)).WaitForAsync();

        var otherLanguage = (language == "ru") ? "en" : "ru";

        // Act
        await Page.Locator(".admiral-language-switcher button").Filter(
            new LocatorFilterOptions { HasText = otherLanguage.ToUpperInvariant() }).ClickAsync();

        // Assert
        await Page.Locator(
            $".admiral-language-switcher button[aria-pressed]:has-text(\"{otherLanguage.ToUpperInvariant()}\")").WaitForAsync();
        await Page.Locator(
            $".admiral-language-switcher button:has-text(\"{otherLanguage.ToUpperInvariant()}\")").WaitForAsync();

        Assert.That(
            Enumerable.Contains(
                new[] { "true", "" },
                await Page.Locator(
                    $".admiral-language-switcher button:has-text(\"{otherLanguage.ToUpperInvariant()}\")").GetAttributeAsync("aria-pressed")),
            Is.True);
        await brand.GetByText(new Regex(theme)).WaitForAsync();

        // Act
        await Page.ReloadAsync();

        // Assert
        await brand.GetByText(new Regex(theme)).WaitForAsync();
        var cookies = await Context.CookiesAsync(BaseUrl);

        Assert.That(cookies.FirstOrDefault((cookie) => cookie.Name == "RadzenAdmiralTheme")?.Value, Is.EqualTo(theme));
        Assert.That(
            Uri.UnescapeDataString(
                cookies.FirstOrDefault((cookie) => cookie.Name == ".AspNetCore.Culture")?.Value ?? "").Contains($"uic={otherLanguage}"),
            Is.True);
    }

    [TestCase("admiral", "ru", TestName = "DocsIteration: Sidebar search filters, clears and handles empty state / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsIteration: Sidebar search filters, clears and handles empty state / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsIteration: Sidebar search filters, clears and handles empty state / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsIteration: Sidebar search filters, clears and handles empty state / admiral-dark / en")]
    public async Task SidebarSearchFiltersClearsAndHandlesEmptyStateAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await Context.AddCookiesAsync(
            [new()
            {
                Name = ".AspNetCore.Culture",
                Value = $"c={language}|uic={language}",
                Url = BaseUrl
            }]);
        await documentation.NavigateAsync("/");
        var search = Page.GetByTestId("component-search");

        // Act
        await search.FillAsync("Button");

        var sidebar = Page.Locator(".admiral-side-menu");

        // Assert
        await sidebar.GetByRole(
            AriaRole.Link,
            new LocatorGetByRoleOptions { Name = "Button", Exact = true }).WaitForAsync();

        var clear = Page.GetByTestId("component-search-clear");

        // Act
        await clear.FocusAsync();
        await clear.PressAsync("Enter");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"component-search\"]')?.value === ''");

        Assert.That(
            await search.EvaluateAsync<bool>("element => document.activeElement === element"),
            Is.EqualTo(true),
            "Clear returns keyboard focus to component search");

        // Act
        await search.FillAsync("NoSuchAdmiralComponent123");

        // Assert
        await Page.GetByTestId("component-search-empty").WaitForAsync();

        // Act
        await Page.GetByTestId("component-search-clear").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"component-search\"]')?.value === ''");

        // Act
        await search.FillAsync((language == "ru") ? "флажок" : "Checkbox");

        // Assert
        await sidebar.GetByRole(
            AriaRole.Link,
            new LocatorGetByRoleOptions { Name = "Checkbox", Exact = true }).WaitForAsync();

        // Act
        await Page.GetByTestId("component-search-clear").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-testid=\"component-search\"]')?.value === ''");

        // Act
        await search.FillAsync("Button");

        var link = sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Button", Exact = true });

        // Act
        await link.FocusAsync();
        await link.PressAsync("Enter");
        await Page.WaitForURLAsync("**/components/button");
    }

    [TestCase("admiral", "ru", TestName = "DocsIteration: Checkbox Admiral and direct Radzen examples respond / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsIteration: Checkbox Admiral and direct Radzen examples respond / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsIteration: Checkbox Admiral and direct Radzen examples respond / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsIteration: Checkbox Admiral and direct Radzen examples respond / admiral-dark / en")]
    public async Task CheckboxAdmiralAndDirectRadzenExamplesRespondAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.NavigateAsync("/components/checkbox");
        var admiral = Page.GetByTestId("checkbox-admiral-demo");
        await admiral.WaitForAsync();
        var admiralResult = Page.GetByTestId("checkbox-admiral-result");
        var before = await admiralResult.InnerTextAsync();
        var check = admiral.GetByRole(AriaRole.Checkbox).First;

        // Act
        await check.FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "previous => document.querySelector('[data-testid=\"checkbox-admiral-result\"]')?.textContent !== previous",
            before);

        // Act
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

        var radzen = Page.GetByTestId("checkbox-radzen-demo");

        // Assert
        await radzen.WaitForAsync();

        var radzenResult = Page.GetByTestId("checkbox-radzen-result");

        // Assert
        var beforeRadzen = await radzenResult.InnerTextAsync();

        // Act
        await radzen.GetByRole(AriaRole.Checkbox).First.ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "previous => document.querySelector('[data-testid=\"checkbox-radzen-result\"]')?.textContent !== previous",
            beforeRadzen);
    }

    [TestCase("admiral", "ru", TestName = "DocsIteration: Table Review action stays accessible across widths / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsIteration: Table Review action stays accessible across widths / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsIteration: Table Review action stays accessible across widths / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsIteration: Table Review action stays accessible across widths / admiral-dark / en")]
    public async Task TableReviewActionStaysAccessibleAcrossWidthsAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.NavigateAsync("/tables");
        var table = Page.Locator(".admiral-table-playground__table");
        var review = table.Locator(".admiral-data-grid__row-actions button").First;
        await review.WaitForAsync();

        // Assert
        Assert.That(
            await review.GetAttributeAsync("aria-label"),
            Is.Not.Null.And.Not.Empty,
            "Review action has an accessible name");

        // Act & assert
        foreach (var width in new[]
        {
            1440,
            1280,
            1024,
            600
        })
        {
            // Act
            await Page.SetViewportSizeAsync(width, 900);
            await Page.WaitForTimeoutAsync(350);

            // Assert
            var scroll = Page.GetByTestId("table-playground-scroll");
            if (width == 1280)
            {
                // Assert
                await scroll.EvaluateAsync("element => { element.scrollLeft = 0; }");
                var initialButton = await review.GetBoundsAsync();
                var initialViewport = await scroll.GetBoundsAsync();

                Assert.That(
                    initialButton.X >= initialViewport.X - 1
                        && initialButton.X + initialButton.Width <= initialViewport.X + initialViewport.Width + 1,
                    Is.True,
                    "Review is fully visible at 1280px without horizontal scrolling");
            }

            // Act
            await review.ScrollIntoViewIfNeededAsync();
            await review.FocusAsync();

            // Assert
            Assert.That(await review.IsVisibleAsync(), Is.EqualTo(true), $"Review visible at {width}px");

            var box = await review.GetBoundsAsync();

            Assert.That(box.Width > 0 && box.Height > 0, Is.True, $"Review has a visible box at {width}px");

            var scrollBox = await scroll.GetBoundsAsync();

            Assert.That(
                box.X >= scrollBox.X - 1 && box.X + box.Width <= scrollBox.X + scrollBox.Width + 1,
                Is.True,
                $"Review is fully inside the table viewport at {width}px");

            // Act
            await review.PressAsync("Enter");

            // Assert
            await Page.WaitForFunctionAsync(
                """
                () => {
                  const result =
                    document.querySelector('.admiral-table-playground__preview .admiral-component-page__result')
                      ?.textContent ?? '';
                  return /action: (Review|Проверка)/i.test(result) && /event: review/i.test(result);
                }
                """);
        }
    }

    [TestCase("admiral", "ru", TestName = "DocsIteration: PaginationOne full and simple modes appear in playground / admiral / ru")]
    [TestCase("admiral", "en", TestName = "DocsIteration: PaginationOne full and simple modes appear in playground / admiral / en")]
    [TestCase("admiral-dark", "ru", TestName = "DocsIteration: PaginationOne full and simple modes appear in playground / admiral-dark / ru")]
    [TestCase("admiral-dark", "en", TestName = "DocsIteration: PaginationOne full and simple modes appear in playground / admiral-dark / en")]
    public async Task PaginationOneFullAndSimpleModesAppearInPlaygroundAsync(string theme, string language)
    {
        // Arrange
        await StartPageAsync(theme, language);
        var documentation = new DocumentationPage(Page);
        await documentation.NavigateAsync("/tables");
        var select = Page.Locator(".admiral-table-playground__controls select").Filter(
            new LocatorFilterOptions { Has = Page.Locator("option[value=\"PaginationOne\"]") });

        // Act
        await select.SelectOptionAsync("PaginationOne");
        await Page.WaitForTimeoutAsync(350);

        var preview = Page.Locator(".admiral-table-playground__preview");

        // Assert
        await preview.Locator(".admiral-pagination-one:not(.admiral-pagination-one--simple)").WaitForAsync();

        await Assertions.Expect(preview.Locator(".admiral-pagination-one__page-size")).ToBeVisibleAsync();

        // Act
        await select.SelectOptionAsync("PaginationOneSimple");

        // Assert
        await preview.Locator(".admiral-pagination-one--simple").WaitForAsync();

        await Assertions.Expect(preview.Locator(".admiral-pagination-one__page-size")).ToHaveCountAsync(0);
    }
}
