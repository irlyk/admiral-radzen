using Admiral.Playwright.Pages.Components.Selection;

namespace Admiral.Playwright.Components.Selection;

[Category("Combo")]
[Category("GlobalSearch")]
public sealed class GlobalSearchTests : GalleryTest
{
    [TestCase("admiral", TestName = "Combo: GlobalSearch: Enter submits typed query without a suggestion; no double submit / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: GlobalSearch: Enter submits typed query without a suggestion; no double submit / admiral-dark")]
    public async Task GlobalSearchEnterSubmitsTypedQueryWithoutASuggestionNoDoubleSubmitAsync(string theme)
    {
        // Arrange: load the search demo and its result output.
        await StartPageAsync(theme, "en");
        var globalSearch = new GlobalSearchPage(Page);
        await globalSearch.NavigateAsync("/regression/overview");

        // Act
        await globalSearch.Input.FillAsync("нет совпадений");
        await globalSearch.PressKeyAsync("Enter");

        // Assert
        await globalSearch.Global.Locator("[role=\"listbox\"]").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(await globalSearch.Outcome.InnerTextAsync(), Is.EqualTo("Search: нет совпадений"));
        Assert.That(
            await globalSearch.Input.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
    }

    [TestCase("admiral", TestName = "Combo: GlobalSearch: active descendant, ArrowUp starts at last result, no page scroll / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: GlobalSearch: active descendant, ArrowUp starts at last result, no page scroll / admiral-dark")]
    public async Task GlobalSearchActiveDescendantArrowUpStartsAtLastResultNoPageScrollAsync(string theme)
    {
        // Arrange: load the search demo and its result output.
        await StartPageAsync(theme, "en");
        var globalSearch = new GlobalSearchPage(Page);
        await globalSearch.NavigateAsync("/regression/overview");
        await globalSearch.Input.ScrollIntoViewIfNeededAsync();

        // Act
        await globalSearch.Input.FocusAsync();

        // Assert
        var y = await Page.EvaluateAsync<double>("() => scrollY");

        // Act
        await globalSearch.PressKeyAsync("ArrowUp");

        // Assert
        var id = await globalSearch.Input.GetAttributeAsync("aria-activedescendant");

        Assert.That((id ?? "").EndsWith("-option-4"), Is.True);

        // Act
        await globalSearch.PressKeyAsync("ArrowDown");

        // Assert
        id = await globalSearch.Input.GetAttributeAsync("aria-activedescendant");

        Assert.That((id ?? "").EndsWith("-option-0"), Is.True);
        Assert.That(await Page.EvaluateAsync<double>("() => scrollY"), Is.EqualTo(y));

        // Act
        await globalSearch.PressKeyAsync("Enter");

        // Assert
        Assert.That(await globalSearch.Outcome.InnerTextAsync(), Is.EqualTo("Selected: payment-1042"));
        Assert.That(
            await globalSearch.Input.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
        await Assertions.Expect(globalSearch.Global.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);
    }

    [TestCase("admiral", TestName = "Combo: GlobalSearch: mouse selection, clear focus, native button Space and Tab exit / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: GlobalSearch: mouse selection, clear focus, native button Space and Tab exit / admiral-dark")]
    public async Task GlobalSearchMouseSelectionClearFocusNativeButtonSpaceAndTabExitAsync(string theme)
    {
        // Arrange: load the search demo and its result output.
        await StartPageAsync(theme, "en");
        var globalSearch = new GlobalSearchPage(Page);
        await globalSearch.NavigateAsync("/regression/overview");

        // Act
        await globalSearch.Input.FillAsync("ADM");
        await globalSearch.Global.GetByRole(AriaRole.Option).Nth(1).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[data-global-search-result]')?.textContent?.includes('payment-1043')");

        Assert.That(await globalSearch.Outcome.InnerTextAsync(), Is.EqualTo("Selected: payment-1043"));

        // Act
        await globalSearch.Global.Locator(".admiral-global-search__clear").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('.admiral-global-search input')?.value === ''");

        Assert.That(await globalSearch.Input.InputValueAsync(), Is.EqualTo(""));
        await Page.WaitForFunctionAsync(
            "() => document.activeElement?.classList.contains('admiral-global-search__input')");

        // Act
        await globalSearch.Input.FillAsync("Aurora");

        // Assert
        await globalSearch.Global.Locator(".admiral-global-search__clear").WaitForAsync();

        // Act
        await globalSearch.PressKeyAsync("Tab");

        // Assert
        Assert.That(
            await globalSearch.Global.Locator(".admiral-global-search__clear").EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true),
            "First Tab must focus clear");

        // Act
        await globalSearch.PressKeyAsync("Tab");

        // Assert
        Assert.That(
            await globalSearch.Global.Locator(".admiral-global-search__submit").EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true),
            "Second Tab must focus submit");

        // Act
        await globalSearch.PressKeyAsync("Space");

        // Assert
        Assert.That(await globalSearch.Outcome.InnerTextAsync(), Is.EqualTo("Search: Aurora"));

        // Act
        await globalSearch.Input.FocusAsync();
        await globalSearch.PressKeyAsync("Tab");
        await globalSearch.PressKeyAsync("Tab");
        await globalSearch.PressKeyAsync("Tab");

        // Assert
        await globalSearch.Global.Locator("[role=\"listbox\"]").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => !!document.activeElement.closest('.admiral-global-search')"),
            Is.EqualTo(false));
    }

    [TestCase("admiral", TestName = "Combo: GlobalSearch: Escape/outside focus; composition Enter does not submit / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: GlobalSearch: Escape/outside focus; composition Enter does not submit / admiral-dark")]
    public async Task GlobalSearchEscapeOutsideFocusCompositionEnterDoesNotSubmitAsync(string theme)
    {
        // Arrange: load the search demo and its result output.
        await StartPageAsync(theme, "en");
        var globalSearch = new GlobalSearchPage(Page);
        await globalSearch.NavigateAsync("/regression/overview");

        // Act
        await globalSearch.Input.FillAsync("ADM");
        await globalSearch.Input.DispatchEventAsync(
            "keydown",
            new { key = "Enter", code = "Enter", isComposing = true });

        // Assert
        Assert.That(await globalSearch.Outcome.InnerTextAsync(), Is.EqualTo("—"));

        // Act
        await globalSearch.PressKeyAsync("Escape");

        // Assert
        await Assertions.Expect(globalSearch.Global.GetByRole(AriaRole.Listbox)).ToHaveCountAsync(0);

        // Act
        await globalSearch.PressKeyAsync("ArrowDown");
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Open drawer", Exact = true }).FocusAsync();

        // Assert
        await globalSearch.Global.Locator("[role=\"listbox\"]").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    [TestCase("admiral", TestName = "Combo: GlobalSearch: clear/submit focus visible, no hover background on clear / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: GlobalSearch: clear/submit focus visible, no hover background on clear / admiral-dark")]
    public async Task GlobalSearchClearSubmitFocusVisibleNoHoverBackgroundOnClearAsync(string theme)
    {
        // Arrange: load the search demo and its result output.
        await StartPageAsync(theme, "en");
        var globalSearch = new GlobalSearchPage(Page);
        await globalSearch.NavigateAsync("/regression/overview");

        // Act
        await globalSearch.Input.FillAsync("ADM");

        var clear = globalSearch.Global.Locator(".admiral-global-search__clear");

        // Assert
        await clear.WaitForAsync();

        // Act
        await globalSearch.PressKeyAsync("Tab");
        await clear.HoverAsync();

        // Assert
        Assert.That(await clear.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        var style = await clear.ReadSnapshotAsync<FocusColorStyle>(
            "e => ({ outline: getComputedStyle(e).outlineStyle, bg: getComputedStyle(e).backgroundColor })");

        Assert.That(style.Outline, Is.EqualTo("solid"));
        Assert.That(style.Bg, Is.EqualTo("rgba(0, 0, 0, 0)"));

        // Act
        await globalSearch.PressKeyAsync("Tab");

        // Assert
        var focus = await globalSearch.Global.Locator(".admiral-global-search__submit").ReadSnapshotAsync<FocusColorStyle>(
            "e => ({ outline: getComputedStyle(e).outlineColor, bg: getComputedStyle(e).backgroundColor })");

        Assert.That(focus.Outline, Is.Not.EqualTo(focus.Bg));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/global-search-{theme}.png" });
    }
}
