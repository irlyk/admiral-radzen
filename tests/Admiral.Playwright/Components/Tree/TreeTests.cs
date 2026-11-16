using Admiral.Playwright.Pages.Components.Tree;

namespace Admiral.Playwright.Components.Tree;

[Category("Tree")]
public sealed class TreeTests : GalleryTest
{
    [TestCase("admiral", TestName = "Tree: M/S geometry, hierarchy metadata, icons and wrapping / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: M/S geometry, hierarchy metadata, icons and wrapping / admiral-dark")]
    public async Task MSGeometryHierarchyMetadataIconsAndWrappingAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act & assert
        foreach (var (size, height, icon, indent) in new[]
        {
            ("Medium", 40, 24, 40),
            ("Small", 32, 20, 36)
        })
        {
            // Act
            await Page.Locator(".admiral-component-controls select").SelectOptionAsync(size);

            // Assert
            await Page.WaitForFunctionAsync(
                """
                ({ height, size }) => {
                  const root = document.querySelector('[aria-label="Документы компании"]');
                  return (
                    root?.classList.contains('admiral-tree-view--s') === (size === 'Small') &&
                    root.querySelector('[data-tree-id="docs"]')?.getBoundingClientRect().height === height
                  );
                }
                """,
                new { height = height, size = size });
            var dimensions = await tree.GetRow("docs").ReadSnapshotAsync<TreeItemGeometry>(
                "e => ({ height: e.getBoundingClientRect().height, icon: e.querySelector('.admiral-tree-view__icon').getBoundingClientRect().width, padding: parseFloat(getComputedStyle(e).paddingInlineStart) })");

            Assert.That(dimensions.Height, Is.EqualTo(height));
            Assert.That(dimensions.Icon, Is.EqualTo(icon));

            var expectedIconColor = (theme == "admiral-dark") ? "rgb(130, 134, 139)" : "rgb(116, 118, 123)";

            Assert.That(
                await tree.GetRow("docs").Locator(".admiral-tree-view__icon").GetCssAsync("color"),
                Is.EqualTo(expectedIconColor));
            Assert.That(
                await tree.GetRow("docs").Locator(".admiral-tree-view__expand").GetCssAsync("color"),
                Is.EqualTo(expectedIconColor));
            Assert.That(
                await tree.GetRow("reports").EvaluateAsync<double>("e => parseFloat(getComputedStyle(e).paddingInlineStart)"),
                Is.EqualTo(dimensions.Padding + indent));
            Assert.That(await tree.GetRow("month").GetAttributeAsync("aria-level"), Is.EqualTo("3"));
            Assert.That(await tree.GetRow("year").GetAttributeAsync("aria-posinset"), Is.EqualTo("2"));
            Assert.That(await tree.GetRow("year").GetAttributeAsync("aria-setsize"), Is.EqualTo("2"));
        }

        // Act
        await Page.SetViewportSizeAsync(700, 900);

        // Assert
        Assert.That(await tree.Tree.EvaluateAsync<bool>("e => e.scrollWidth <= e.clientWidth + 1"), Is.True);
    }

    [TestCase("admiral", TestName = "Tree: cascade checks descendants and parents; disabled branches excluded / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: cascade checks descendants and parents; disabled branches excluded / admiral-dark")]
    public async Task CascadeChecksDescendantsAndParentsDisabledBranchesExcludedAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await tree.GetRow("docs").ClickAsync();

        // Assert
        foreach (var id in new[]
        {
            "docs",
            "reports",
            "month",
            "year",
            "notes"
        })
        {
            // Assert
            await tree.WaitForRowAttributeAsync(id, "aria-checked", "true");
        }

        foreach (var id in new[]
        {
            "archive",
            "locked",
            "locked-child"
        })
        {
            // Assert
            await tree.WaitForRowAttributeAsync(id, "aria-checked", "false");
        }

        // Act
        await tree.GetRow("year").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("year", "aria-checked", "false");
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "mixed");
        await tree.WaitForRowAttributeAsync("docs", "aria-checked", "mixed");

        // Act
        await tree.GetRow("reports").Locator(".admiral-tree-view__checkbox").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "true");
        await tree.WaitForRowAttributeAsync("year", "aria-checked", "true");
    }

    [TestCase("admiral", TestName = "Tree: strict mode checks only the selected node / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: strict mode checks only the selected node / admiral-dark")]
    public async Task StrictModeChecksOnlyTheSelectedNodeAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act: enable strict checking without ancestor propagation.
        await Page.GetByLabel("CheckStrictly", new PageGetByLabelOptions { Exact = true }).CheckAsync();
        await tree.GetRow("reports").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "true");
        await tree.WaitForRowAttributeAsync("month", "aria-checked", "false");
        await tree.WaitForRowAttributeAsync("docs", "aria-checked", "false");

        // Act: toggle the parent independently.
        await tree.GetRow("reports").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "false");
    }

    [TestCase("admiral", TestName = "Tree: single selection, keyboard parent/child navigation and selected-child collapse / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: single selection, keyboard parent/child navigation and selected-child collapse / admiral-dark")]
    public async Task SingleSelectionKeyboardParentChildNavigationAndSelectedChildCollapseAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Arrange: enable single selection and focus the hierarchy.
        await Page.GetByLabel("AllowCheckBoxes", new PageGetByLabelOptions { Exact = true }).UncheckAsync();
        await tree.GetRow("month").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("month", "aria-selected", "true");
        await tree.WaitForFocusAsync("month");

        // Act: move from the child to its parent.
        await Page.Keyboard.PressAsync("ArrowLeft");

        // Assert
        await tree.WaitForFocusAsync("reports");

        // Act: collapse the parent branch.
        await Page.Keyboard.PressAsync("ArrowLeft");

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-expanded", "false");

        await Assertions.Expect(tree.GetRow("month")).ToHaveCountAsync(0);

        // Act: expand the branch again.
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-expanded", "true");
        await tree.WaitForRowAttributeAsync("month", "aria-selected", "true");

        // Act: enter the expanded branch.
        await Page.Keyboard.PressAsync("ArrowRight");

        // Assert
        await tree.WaitForFocusAsync("month");

        // Act: move to the next visible node.
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await tree.WaitForFocusAsync("year");

        // Act: skip disabled nodes during navigation.
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await tree.WaitForFocusAsync("notes");

        // Act: move to the last enabled node.
        await Page.Keyboard.PressAsync("End");

        // Assert
        await tree.WaitForFocusAsync("settings");

        // Act: return to the first node.
        await Page.Keyboard.PressAsync("Home");

        // Assert
        await tree.WaitForFocusAsync("docs");

        // Act: navigate without changing the selected value.
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await tree.WaitForFocusAsync("reports");

        // Act: select the focused node with Space.
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-selected", "true");

        await Assertions.Expect(tree.Tree.Locator(".rz-chkbox")).ToHaveCountAsync(0);
    }

    [TestCase("admiral", TestName = "Tree: Enter/Space, letter navigation, Tab exit and no page scroll on arrows / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: Enter/Space, letter navigation, Tab exit and no page scroll on arrows / admiral-dark")]
    public async Task EnterSpaceLetterNavigationTabExitAndNoPageScrollOnArrowsAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await tree.GetRow("docs").FocusAsync();

        // Assert
        var scrollY = await Page.EvaluateAsync<double>("() => scrollY");

        // Act
        await Page.Keyboard.PressAsync("ArrowDown");

        // Assert
        await tree.WaitForFocusAsync("reports");

        Assert.That(await Page.EvaluateAsync<double>("() => scrollY"), Is.EqualTo(scrollY));

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "true");

        // Act
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "false");

        // Act
        await tree.GetRow("reports").DispatchEventAsync("keydown", new { key = "н", code = "KeyY", bubbles = true });

        // Assert
        await tree.WaitForFocusAsync("settings");

        // Act
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        Assert.That(await tree.Tree.EvaluateAsync<bool>("e => e.contains(document.activeElement)"), Is.EqualTo(false));
    }

    [TestCase("admiral", TestName = "Tree: disabled node and inherited-disabled subtree cannot mutate or collapse / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: disabled node and inherited-disabled subtree cannot mutate or collapse / admiral-dark")]
    public async Task DisabledNodeAndInheritedDisabledSubtreeCannotMutateOrCollapseAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await tree.GetRow("archive").ClickAsync(new LocatorClickOptions { Force = true });

        // Assert
        await tree.WaitForRowAttributeAsync("archive", "aria-selected", "false");
        await tree.WaitForRowAttributeAsync("archive", "aria-checked", "false");

        // Act
        await tree.GetRow("locked-child").ClickAsync(new LocatorClickOptions { Force = true });

        // Assert
        await tree.WaitForRowAttributeAsync("locked-child", "aria-disabled", "true");
        await tree.WaitForRowAttributeAsync("locked-child", "aria-selected", "false");

        // Act
        await tree.GetRow("locked").Locator("button").DispatchEventAsync("click");

        // Assert
        await tree.WaitForRowAttributeAsync("locked", "aria-expanded", "true");

        // Act
        await Page.GetByLabel("Disabled", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("docs", "aria-disabled", "true");

        // Act
        await tree.GetRow("docs").DispatchEventAsync("click");

        // Assert
        await tree.WaitForRowAttributeAsync("docs", "aria-checked", "false");

        // Act
        await tree.GetRow("docs").FocusAsync();
        await Page.Keyboard.PressAsync("Space");

        // Assert
        await tree.WaitForRowAttributeAsync("docs", "aria-checked", "false");
    }

    [TestCase("admiral", TestName = "Tree: expansion alone never checks/selects and external reset is applied / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: expansion alone never checks/selects and external reset is applied / admiral-dark")]
    public async Task ExpansionAloneNeverChecksSelectsAndExternalResetIsAppliedAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await tree.GetRow("reports").Locator("button").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-expanded", "false");
        await tree.WaitForRowAttributeAsync("reports", "aria-selected", "false");
        await tree.WaitForRowAttributeAsync("reports", "aria-checked", "false");

        // Act
        await tree.GetRow("reports").Locator("button").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("reports", "aria-expanded", "true");

        // Act
        await tree.GetRow("month").ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("month", "aria-checked", "true");

        // Act
        await Page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions { Name = "Сбросить", Exact = true }).ClickAsync();

        // Assert
        await tree.WaitForRowAttributeAsync("month", "aria-checked", "false");
        await tree.WaitForRowAttributeAsync("month", "aria-selected", "false");
    }

    [TestCase("admiral", TestName = "Tree: hover/focus/hover+focus/active/selected and disabled colors / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: hover/focus/hover+focus/active/selected and disabled colors / admiral-dark")]
    public async Task HoverFocusHoverFocusActiveSelectedAndDisabledColorsAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();
        var target = tree.GetRow("reports");

        // Act
        await Page.Mouse.MoveAsync((float)0, (float)0);

        // Assert
        var normal = await target.GetCssAsync("background-color");

        // Act
        await target.HoverAsync();

        // Assert
        var hover = await target.GetCssAsync("background-color");

        Assert.That(hover, Is.Not.EqualTo(normal));

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await target.FocusAsync();

        // Assert
        Assert.That(await target.GetCssAsync("outline-style"), Is.EqualTo("solid"));

        // Act
        await Page.Mouse.DownAsync();

        // Assert
        var active = await target.GetCssAsync("background-color");

        // Act
        await Page.Mouse.UpAsync();

        // Assert
        Assert.That(active, Is.Not.EqualTo(hover));
        await tree.WaitForRowAttributeAsync("reports", "aria-selected", "true");

        // Act
        await Page.Mouse.MoveAsync((float)0, (float)0);

        // Assert
        var selected = await target.GetCssAsync("background-color");

        Assert.That(selected, Is.Not.EqualTo(normal));

        var disabled = tree.GetRow("archive");

        // Assert
        var color = await disabled.GetCssAsync("color");

        Assert.That(color, Is.Not.EqualTo(await target.GetCssAsync("color")));
        await Page.WaitForTimeoutAsync(350);

        // Diagnostics
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{Output}/tree-{theme}.png", FullPage = true });
    }

    [TestCase("admiral", TestName = "Tree: template, empty state and accessible root survive parameter changes / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: template, empty state and accessible root survive parameter changes / admiral-dark")]
    public async Task TemplateEmptyStateAndAccessibleRootSurviveParameterChangesAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await Page.GetByLabel("ItemTemplate", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await tree.Tree.Locator("small").First.WaitForAsync();

        // Act
        await Page.GetByLabel("Пустое состояние", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await tree.Tree.GetByText("Нет элементов", new LocatorGetByTextOptions { Exact = true }).WaitForAsync();

        Assert.That(await tree.Tree.GetAttributeAsync("tabindex"), Is.EqualTo("0"));

        // Act
        await Page.GetByLabel("Пустое состояние", new PageGetByLabelOptions { Exact = true }).UncheckAsync();

        // Assert
        await tree.GetRow("docs").WaitForAsync();

        await Assertions.Expect(tree.Tree.Locator("[role=\"treeitem\"][tabindex=\"0\"]")).ToHaveCountAsync(1);
    }

    [TestCase("admiral", TestName = "Tree: API and independent direct Radzen tree remain runnable / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: API and independent direct Radzen tree remain runnable / admiral-dark")]
    public async Task APIAndIndependentDirectRadzenTreeRemainRunnableAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"api\"]").ClickAsync();

        // Assert
        await Page.GetByRole(
            AriaRole.Table,
            new PageGetByRoleOptions { Name = "Состояния и модель", Exact = true }).WaitForAsync();

        // Act
        await Page.Locator(".component-doc-tabs [data-doc-tab=\"theme\"]").ClickAsync();

        var direct = Page.Locator(".rz-tree.admiral-tree");

        // Assert
        await direct.WaitForAsync();

        // Act
        await Page.GetByLabel("AllowCheckBoxes", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await direct.Locator(".rz-chkbox").First.WaitForAsync();

        // Act
        await Page.GetByLabel("Маленькое", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        await Page.WaitForFunctionAsync("() => !!document.querySelector('.rz-tree.admiral-tree--small')");
    }

    [TestCase("admiral", TestName = "Tree: uncontrolled state survives unrelated parent renders / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: uncontrolled state survives unrelated parent renders / admiral-dark")]
    public async Task UncontrolledStateSurvivesUnrelatedParentRendersAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();
        var local = Page.GetByRole(
            AriaRole.Tree,
            new PageGetByRoleOptions { Name = "Локальное состояние", Exact = true });
        ILocator GetLocalRow(string id) => local.Locator($"[data-tree-id=\"{id}\"]");

        // Act
        await GetLocalRow("month").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[aria-label=\"Локальное состояние\"] [data-tree-id=\"month\"]')?.getAttribute('aria-checked') === 'true'");

        // Act
        await GetLocalRow("reports").Locator("button").ClickAsync();

        // Assert
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[aria-label=\"Локальное состояние\"] [data-tree-id=\"reports\"]')?.getAttribute('aria-expanded') === 'false'");

        // Act
        await Page.GetByLabel("ItemTemplate", new PageGetByLabelOptions { Exact = true }).CheckAsync();

        // Assert
        Assert.That(await GetLocalRow("reports").GetAttributeAsync("aria-expanded"), Is.EqualTo("false"));

        // Act
        await GetLocalRow("reports").Locator("button").ClickAsync();

        // Assert
        await GetLocalRow("month").WaitForAsync();

        Assert.That(await GetLocalRow("month").GetAttributeAsync("aria-checked"), Is.EqualTo("true"));
        Assert.That(await GetLocalRow("month").GetAttributeAsync("aria-selected"), Is.EqualTo("true"));
    }

    [TestCase("admiral", TestName = "Tree: IME composition and modifier shortcuts do not mutate selection / admiral")]
    [TestCase("admiral-dark", TestName = "Tree: IME composition and modifier shortcuts do not mutate selection / admiral-dark")]
    public async Task IMECompositionAndModifierShortcutsDoNotMutateSelectionAsync(string theme)
    {
        // Arrange: load the tree demo and its hierarchy.
        await StartPageAsync(theme, "ru");
        var tree = new TreePage(Page);
        await tree.InitializeAsync();

        // Act
        await tree.GetRow("docs").FocusAsync();
        await tree.GetRow("docs").DispatchEventAsync(
            "keydown",
            new { key = "Enter", code = "Enter", isComposing = true, bubbles = true });

        // Assert
        await tree.WaitForRowAttributeAsync("docs", "aria-checked", "false");

        // Act
        await Page.Keyboard.PressAsync("Control+ArrowDown");

        // Assert
        await tree.WaitForFocusAsync("docs");

        // Act
        await Page.Keyboard.PressAsync("Meta+ArrowDown");

        // Assert
        await tree.WaitForFocusAsync("docs");

        // Act
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await tree.WaitForRowAttributeAsync("docs", "aria-checked", "true");
    }
}
