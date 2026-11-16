using Admiral.Playwright.Pages.Components.Selection;

namespace Admiral.Playwright.Components.Selection;

[Category("Combo")]
[Category("TreeSelect")]
public sealed class TreeSelectTests : GalleryTest
{
    [TestCase("admiral", TestName = "Combo: TreeSelect: keyboard opening, cascade normalization and mixed ARIA / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: keyboard opening, cascade normalization and mixed ARIA / admiral-dark")]
    public async Task TreeSelectKeyboardOpeningCascadeNormalizationAndMixedARIAAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.FocusAsync();
        await treeSelect.PressKeyAsync("Space");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");
        await treeSelect.WaitForNodeCheckedAsync("Finance", "mixed");

        // Act
        await treeSelect.PressKeyAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "true");

        // Act
        await treeSelect.PressKeyAsync("ArrowRight");
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Payments");

        // Act
        await treeSelect.PressKeyAsync("ArrowRight");
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Incoming");

        // Act
        await treeSelect.PressKeyAsync("Space");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Incoming", "false");
        await treeSelect.WaitForNodeCheckedAsync("Payments", "mixed");
        await treeSelect.WaitForNodeCheckedAsync("Finance", "mixed");

        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await treeSelect.Tree.Locator(".admiral-tree-select__clear").ClickAsync();

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "false");

        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: chip strategies preserve cascade selection / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: chip strategies preserve cascade selection / admiral-dark")]
    public async Task TreeSelectChipStrategiesPreserveCascadeSelectionAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.PressAsync("Enter");
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "true");

        // Act
        await treeSelect.Trigger.PressAsync("Escape");

        // Assert
        var values = await Page.Locator(".admiral-component-page__result").InnerTextAsync();
        var events = await Page.Locator("[data-tree-events]").InnerTextAsync();

        async Task ExpectChipsAsync(string[] expected)
        {
            await Page.WaitForFunctionAsync(
                """
                (expected) =>
                  JSON.stringify(
                    [...document.querySelectorAll('.admiral-tree-select__chip-label')].map((e) => e.textContent),
                  ) === JSON.stringify(expected)
                """,
                expected);
        }

        // Assert
        await ExpectChipsAsync(new[] { "Finance" });

        // Act
        await Page.GetByRole(
            AriaRole.Combobox,
            new PageGetByRoleOptions { Name = "ShowCheckedStrategy", Exact = true }).SelectOptionAsync("All");

        // Assert
        await ExpectChipsAsync(new[] { "Finance", "Payments", "Incoming", "Outgoing", "Reports" });

        // Act
        await Page.GetByRole(
            AriaRole.Combobox,
            new PageGetByRoleOptions { Name = "ShowCheckedStrategy", Exact = true }).SelectOptionAsync("Child");

        // Assert
        await ExpectChipsAsync(new[] { "Incoming", "Outgoing", "Reports" });

        Assert.That(await Page.Locator(".admiral-component-page__result").InnerTextAsync(), Is.EqualTo(values));

        // Act
        await Page.GetByRole(
            AriaRole.Combobox,
            new PageGetByRoleOptions { Name = "ShowCheckedStrategy", Exact = true }).SelectOptionAsync("Parent");

        // Assert
        await ExpectChipsAsync(new[] { "Finance" });

        Assert.That(await Page.Locator("[data-tree-events]").InnerTextAsync(), Is.EqualTo(events));

        // Act
        await treeSelect.Trigger.PressAsync("Enter");
        await treeSelect.Trigger.PressAsync("ArrowRight");
        await treeSelect.Trigger.PressAsync("ArrowDown");
        await treeSelect.Trigger.PressAsync("ArrowRight");
        await treeSelect.Trigger.PressAsync("ArrowDown");
        await treeSelect.Trigger.PressAsync("Space");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Incoming", "false");
        await treeSelect.WaitForNodeCheckedAsync("Finance", "mixed");

        // Act
        await treeSelect.Trigger.PressAsync("Escape");

        // Assert
        await ExpectChipsAsync(new[] { "Outgoing", "Reports" });

        // Act
        await treeSelect.Tree.Locator(".admiral-tree-select__clear").ClickAsync();

        // Assert
        await ExpectChipsAsync(Array.Empty<string>());
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: removing parent chip clears its subtree and restores trigger focus / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: removing parent chip clears its subtree and restores trigger focus / admiral-dark")]
    public async Task TreeSelectRemovingParentChipClearsItsSubtreeAndRestoresTriggerFocusAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.PressKeyAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "true");

        // Act
        await treeSelect.PressKeyAsync("Escape");
        await treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Удалить Finance", Exact = true }).ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.admiral-tree-select__chip').length === 0");

        Assert.That(
            (await Page.Locator(".admiral-component-page__result").InnerTextAsync()).Trim(),
            Is.EqualTo("Selected:"));
        Assert.That(await Page.Locator("[data-tree-events]").InnerTextAsync(), Does.Match(new Regex("Changes: 2")));
        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "false");
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: leaf chip native Space removal normalizes parent selection / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: leaf chip native Space removal normalizes parent selection / admiral-dark")]
    public async Task TreeSelectLeafChipNativeSpaceRemovalNormalizesParentSelectionAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.PressKeyAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "true");

        // Act
        await treeSelect.PressKeyAsync("Escape");
        await Page.GetByRole(
            AriaRole.Combobox,
            new PageGetByRoleOptions { Name = "ShowCheckedStrategy", Exact = true }).SelectOptionAsync("Child");

        var remove = treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Удалить Incoming", Exact = true });

        // Act
        await remove.FocusAsync();
        await remove.HoverAsync();

        // Assert
        var style = await remove.ReadSnapshotAsync<FocusBackgroundStyle>(
            "e => ({ outline: getComputedStyle(e).outlineStyle, background: getComputedStyle(e).backgroundColor })");

        Assert.That(style.Outline, Is.EqualTo("solid"));
        Assert.That(style.Background, Is.EqualTo("rgba(0, 0, 0, 0)"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/tree-select-chips-{theme}.png" });

        // Act
        await treeSelect.PressKeyAsync("Space");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => ![...document.querySelectorAll('.admiral-tree-select__chip-label')].some(e => e.textContent === 'Incoming')");

        Assert.That(await Page.Locator("[data-tree-events]").InnerTextAsync(), Does.Match(new Regex("Changes: 2")));

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Finance", "mixed");

        // Act
        await treeSelect.PressKeyAsync("ArrowRight");
        await treeSelect.PressKeyAsync("ArrowDown");
        await treeSelect.PressKeyAsync("ArrowRight");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Incoming", "false");
        await treeSelect.WaitForNodeCheckedAsync("Outgoing", "true");
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: loading and skeleton block interaction without losing values / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: loading and skeleton block interaction without losing values / admiral-dark")]
    public async Task TreeSelectLoadingAndSkeletonBlockInteractionWithoutLosingValuesAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Loading", Exact = true }).CheckAsync();

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        await Assertions.Expect(treeSelect.Trigger).ToBeDisabledAsync();
        Assert.That(await treeSelect.Tree.GetAttributeAsync("aria-busy"), Is.EqualTo("true"));
        await Assertions.Expect(
            treeSelect.Tree.GetByRole(
                AriaRole.Alert,
                new LocatorGetByRoleOptions { Name = "Загрузка", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(treeSelect.Tree.Locator(".admiral-tree-select__chip-remove")).ToHaveCountAsync(0);

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Loading", Exact = true }).UncheckAsync();
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Skeleton", Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.admiral-tree-select')?.dataset.skeleton === 'true'");

        await Assertions.Expect(treeSelect.Trigger).ToBeDisabledAsync();
        await Assertions.Expect(treeSelect.Tree.Locator(".admiral-tree-select__chip")).ToHaveCountAsync(0);

        var bg = await treeSelect.Tree.Locator(".admiral-tree-select__control").GetCssAsync("background-color");

        Assert.That(bg, Is.Not.EqualTo("rgba(0, 0, 0, 0)"));
        Assert.That(bg, Is.Not.EqualTo("rgb(255, 255, 255)"));

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Skeleton", Exact = true }).UncheckAsync();

        // Assert
        await treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Удалить Incoming", Exact = true }).WaitForAsync();

        Assert.That(
            await Page.Locator(".admiral-component-page__result").InnerTextAsync(),
            Does.Match(new Regex("Incoming")));
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: one chip row does not scroll; removal icons fit and are centered / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: one chip row does not scroll; removal icons fit and are centered / admiral-dark")]
    public async Task TreeSelectOneChipRowDoesNotScrollRemovalIconsFitAndAreCenteredAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxRowCount", Exact = true }).FillAsync("1");
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxRowCount", Exact = true }).PressAsync("Tab");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('.admiral-tree-select__chips')).maxHeight === '24px'");

        // Act & assert
        foreach (var size in new[]
        {
            "Small",
            "Medium",
            "ExtraLarge"
        })
        {
            // Act
            await Page.GetByRole(
                AriaRole.Combobox,
                new PageGetByRoleOptions { Name = "Size", Exact = true }).SelectOptionAsync(size);

            // Assert
            await Page.WaitForFunctionAsync(
                """
                (size) =>
                  document
                    .querySelector('.admiral-tree-select')
                    ?.classList.contains(
                      `admiral-tree-select--${{ Small: 's', Medium: 'm', ExtraLarge: 'xl' }[size]}`,
                    )
                """,
                size);
            var geometry = await treeSelect.Tree.Locator(".admiral-tree-select__chips").ReadSnapshotAsync<TreeSelectChipGeometry>(
                """
                (e) => {
                  const button = e.querySelector('button').getBoundingClientRect();
                  const icon = e.querySelector('.admiral-icon').getBoundingClientRect();
                  const chip = e.querySelector('.admiral-tree-select__chip').getBoundingClientRect();
                  const field = e.closest('.admiral-tree-select__control').getBoundingClientRect();
                  return {
                    scrollHeight: e.scrollHeight,
                    clientHeight: e.clientHeight,
                    scrollWidth: e.scrollWidth,
                    clientWidth: e.clientWidth,
                    buttonWidth: button.width,
                    buttonHeight: button.height,
                    iconWidth: icon.width,
                    iconHeight: icon.height,
                    dx: Math.abs(icon.x + icon.width / 2 - button.x - button.width / 2),
                    dy: Math.abs(icon.y + icon.height / 2 - button.y - button.height / 2),
                    fieldDy: Math.abs(chip.y + chip.height / 2 - field.y - field.height / 2),
                  };
                }
                """);

            Assert.That(
                geometry.ScrollHeight,
                Is.EqualTo(geometry.ClientHeight),
                $"{size}: one row must not scroll vertically");
            Assert.That(
                geometry.ScrollWidth,
                Is.EqualTo(geometry.ClientWidth),
                $"{size}: one row must not scroll horizontally");
            Assert.That(geometry.IconWidth, Is.EqualTo(16));
            Assert.That(geometry.IconHeight, Is.EqualTo(16));
            Assert.That(geometry.ButtonWidth, Is.EqualTo(20));
            Assert.That(geometry.ButtonHeight, Is.EqualTo(20));
            Assert.That(geometry.Dx < 0.5 && geometry.Dy < 0.5, Is.True, $"{size}: icon must be centered");
            Assert.That(geometry.FieldDy < 0.5, Is.True, $"{size}: chip row must be centered within the field");
        }

        // Act
        await treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Удалить Incoming", Exact = true }).FocusAsync();
        await treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Удалить Incoming", Exact = true }).HoverAsync();
        await treeSelect.Tree.ScrollIntoViewIfNeededAsync();

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/tree-select-single-row-{theme}.png" });
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: row limits constrain closed chips and expand with popup / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: row limits constrain closed chips and expand with popup / admiral-dark")]
    public async Task TreeSelectRowLimitsConstrainClosedChipsAndExpandWithPopupAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Select available", Exact = true }).ClickAsync();
        await Page.GetByRole(
            AriaRole.Combobox,
            new PageGetByRoleOptions { Name = "ShowCheckedStrategy", Exact = true }).SelectOptionAsync("All");
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxRowCount", Exact = true }).FillAsync("1");
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MaxRowCount", Exact = true }).PressAsync("Tab");

        var chips = treeSelect.Tree.Locator(".admiral-tree-select__chips");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('.admiral-tree-select__chips')).maxHeight === '24px'");

        Assert.That(await chips.EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"), Is.True);

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        Assert.That(await chips.GetCssAsync("max-height"), Is.EqualTo("none"));

        // Act
        await treeSelect.PressKeyAsync("Escape");
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MinRowCount", Exact = true }).FillAsync("2");
        await Page.GetByRole(
            AriaRole.Spinbutton,
            new PageGetByRoleOptions { Name = "MinRowCount", Exact = true }).PressAsync("Tab");

        // Assert
        await Page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('.admiral-tree-select__chips')).minHeight === '52px'");

        Assert.That(await chips.GetCssAsync("max-height"), Is.EqualTo("52px"));

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/tree-select-rows-{theme}.png" });
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: panel button retains native keyboard behavior and Escape closes / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: panel button retains native keyboard behavior and Escape closes / admiral-dark")]
    public async Task TreeSelectPanelButtonRetainsNativeKeyboardBehaviorAndEscapeClosesAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Panels", Exact = true }).CheckAsync();
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        await Assertions.Expect(
            treeSelect.Tree.GetByText(
                "Choose application sections",
                new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();

        // Act
        await treeSelect.Search.FocusAsync();
        await treeSelect.PressKeyAsync("Tab");

        // Assert
        var button = treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Panel action", Exact = true });

        Assert.That(await button.EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));

        // Act
        await treeSelect.PressKeyAsync("Space");

        // Assert
        Assert.That(
            await Page.Locator("[data-tree-events]").InnerTextAsync(),
            Does.Match(new Regex("Panel action completed")));
        Assert.That(await treeSelect.Trigger.GetAttributeAsync("aria-expanded"), Is.EqualTo("true"));

        // Act
        await treeSelect.PressKeyAsync("Escape");

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.Tree.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Удалить Incoming", Exact = true }).FocusAsync();
        await treeSelect.PressKeyAsync("Escape");

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        // Act
        await treeSelect.Trigger.PressAsync("Enter");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.Tree.Locator(".admiral-tree-select__clear").FocusAsync();
        await treeSelect.PressKeyAsync("Escape");

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: parent/child navigation, Home/End, disabled skip and read-only node / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: parent/child navigation, Home/End, disabled skip and read-only node / admiral-dark")]
    public async Task TreeSelectParentChildNavigationHomeEndDisabledSkipAndReadOnlyNodeAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.FocusAsync();
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.PressKeyAsync("End");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Documents");

        // Act
        await treeSelect.PressKeyAsync("ArrowRight");
        await treeSelect.PressKeyAsync("ArrowRight");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Acts");

        // Act
        await treeSelect.PressKeyAsync("End");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Policies (read only)");
        var before = await Page.Locator(".admiral-component-page__result").InnerTextAsync();

        // Act
        await treeSelect.PressKeyAsync("Enter");

        // Assert
        Assert.That(await Page.Locator(".admiral-component-page__result").InnerTextAsync(), Is.EqualTo(before));

        // Act
        await treeSelect.PressKeyAsync("ArrowUp");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Statements");

        // Act
        await treeSelect.PressKeyAsync("ArrowLeft");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Documents");

        // Act
        await treeSelect.PressKeyAsync("ArrowLeft");

        // Assert
        Assert.That(await treeSelect.GetNode("Documents").GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));

        // Act
        await treeSelect.PressKeyAsync("Home");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.PressKeyAsync("Escape");

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: search retains text keys; Enter selects; disabled branch stays inert / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: search retains text keys; Enter selects; disabled branch stays inert / admiral-dark")]
    public async Task TreeSelectSearchRetainsTextKeysEnterSelectsDisabledBranchStaysInertAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.ClickAsync();
        await treeSelect.Search.FillAsync("Incoming");
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Finance");

        // Act
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Payments");

        // Act
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Incoming");
        var descendant = await treeSelect.Search.GetAttributeAsync("aria-activedescendant");

        // Act
        await treeSelect.PressKeyAsync("Home");
        await treeSelect.PressKeyAsync("ArrowRight");
        await treeSelect.PressKeyAsync("End");

        // Assert
        Assert.That(await treeSelect.Search.InputValueAsync(), Is.EqualTo("Incoming"));
        Assert.That(await treeSelect.Search.GetAttributeAsync("aria-activedescendant"), Is.EqualTo(descendant));

        // Act
        await treeSelect.PressKeyAsync("Enter");

        // Assert
        await treeSelect.WaitForNodeCheckedAsync("Incoming", "false");

        Assert.That(
            await treeSelect.Search.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));

        // Act
        await treeSelect.Search.FillAsync("Archive");
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Documents");

        // Act
        await treeSelect.PressKeyAsync("ArrowDown");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Documents");

        Assert.That(await treeSelect.GetNode("Archive 2025").GetAttributeAsync("aria-disabled"), Is.EqualTo("true"));

        // Act
        await treeSelect.Search.FillAsync("нет совпадений");

        // Assert
        await treeSelect.Tree.GetByText(
            "Ничего не найдено",
            new LocatorGetByTextOptions { Exact = true }).WaitForAsync();

        await Assertions.Expect(treeSelect.Tree.GetByRole(AriaRole.Treeitem)).ToHaveCountAsync(0);

        // Act
        await treeSelect.PressKeyAsync("Enter");
        await treeSelect.PressKeyAsync("Escape");

        // Assert
        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: Tab exits, outside click does not steal focus, read-only/disabled field / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: Tab exits, outside click does not steal focus, read-only/disabled field / admiral-dark")]
    public async Task TreeSelectTabExitsOutsideClickDoesNotStealFocusReadOnlyDisabledFieldAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.ClickAsync();
        await treeSelect.Search.FocusAsync();
        await treeSelect.PressKeyAsync("Tab");

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(
            await Page.EvaluateAsync<bool>(
                "() => !!document.activeElement.closest('.admiral-tree-select')"),
            Is.EqualTo(false),
            "Tab must leave TreeSelect");

        // Act
        await treeSelect.Trigger.ClickAsync();
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Read-only", Exact = true }).CheckAsync();

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForFunctionAsync("() => document.activeElement?.closest('.admiral-component-controls')");

        // Act
        await treeSelect.Trigger.FocusAsync();
        await treeSelect.PressKeyAsync("Space");

        // Assert
        Assert.That(
            await treeSelect.Trigger.GetAttributeAsync("aria-expanded"),
            Is.EqualTo("false"),
            "Read-only trigger must not open");

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Read-only", Exact = true }).UncheckAsync();
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Disabled", Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => document.querySelector('.admiral-tree-select__trigger')?.disabled");

        Assert.That(
            await treeSelect.Trigger.IsDisabledAsync(),
            Is.EqualTo(true),
            "Disabled trigger must be native disabled");
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: single selection closes and restores focus / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: single selection closes and restores focus / admiral-dark")]
    public async Task TreeSelectSingleSelectionClosesAndRestoresFocusAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await Page.GetByRole(
            AriaRole.Checkbox,
            new PageGetByRoleOptions { Name = "Multiple", Exact = true }).UncheckAsync();
        await treeSelect.Trigger.FocusAsync();
        await treeSelect.PressKeyAsync("ArrowUp");

        // Assert
        await treeSelect.WaitForActiveNodeAsync("Documents");

        // Act
        await treeSelect.PressKeyAsync("Enter");

        // Assert
        await treeSelect.Tree.Locator(".admiral-tree-select__popup").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Detached });

        Assert.That(await treeSelect.Trigger.InnerTextAsync(), Does.Match(new Regex("Documents")));
        Assert.That(
            await treeSelect.Trigger.EvaluateAsync<bool>("e => e === document.activeElement"),
            Is.EqualTo(true));
    }

    [TestCase("admiral", TestName = "Combo: TreeSelect: light/dark geometry, hover+focus and narrow viewport / admiral")]
    [TestCase("admiral-dark", TestName = "Combo: TreeSelect: light/dark geometry, hover+focus and narrow viewport / admiral-dark")]
    public async Task TreeSelectLightDarkGeometryHoverFocusAndNarrowViewportAsync(string theme)
    {
        // Arrange: load the tree-select demo and locate its controls.
        await StartPageAsync(theme, "en");
        var treeSelect = new TreeSelectPage(Page);
        await treeSelect.NavigateAsync("/components/tree-select");

        // Act
        await treeSelect.Trigger.FocusAsync();
        await treeSelect.PressKeyAsync("ArrowDown");
        await treeSelect.Trigger.HoverAsync();

        // Assert
        var control = await treeSelect.Tree.Locator(".admiral-tree-select__control").ReadSnapshotAsync<InputControlStyle>(
            """
            (e) => {
              const s = getComputedStyle(e);
              return {
                border: s.borderTopWidth,
                shadow: s.boxShadow,
                height: e.getBoundingClientRect().height,
              };
            }
            """);

        Assert.That(control.Border, Is.EqualTo("1px"));
        Assert.That(control.Shadow, Is.Not.EqualTo("none"));
        Assert.That(control.Height, Is.EqualTo(40));

        var row = treeSelect.GetNode("Finance").Locator(":scope > .admiral-tree-select__row");

        Assert.That(await row.GetCssAsync("outline-style"), Is.EqualTo("solid"));

        var border = await treeSelect.Search.GetCssAsync("border-top-width");

        Assert.That(border, Is.EqualTo("1px"), "Search border must not contain a nested border shorthand");

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/tree-select-{theme}.png" });

        // Act
        await treeSelect.PressKeyAsync("Escape");

        // Act & assert
        foreach (var (size, height) in new[]
        {
            ("Small", 32),
            ("ExtraLarge", 56)
        })
        {
            // Act
            await Page.GetByRole(
                AriaRole.Combobox,
                new PageGetByRoleOptions { Name = "Size", Exact = true }).SelectOptionAsync(size);

            // Assert
            Assert.That(
                await treeSelect.Tree.Locator(".admiral-tree-select__control").EvaluateAsync<double>("e => e.getBoundingClientRect().height"),
                Is.EqualTo(height));
        }

        // Act
        await Page.SetViewportSizeAsync(600, 900);
        await treeSelect.Trigger.ClickAsync();

        // Assert
        var box = await treeSelect.Tree.Locator(".admiral-tree-select__popup").GetBoundsAsync();

        Assert.That(box.X >= 0 && box.X + box.Width <= 600, Is.True);

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/tree-select-{theme}-narrow.png" });

        // Act
        await Page.SetViewportSizeAsync(1440, 1000);
    }
}
